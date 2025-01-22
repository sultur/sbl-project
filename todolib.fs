
s" todostats.csv" 2Constant CSVFILE
variable csvfd
0 csvfd !

\ Helper functions

: count-substr {: addr1 u1 addr2 u2 -- u3 :}
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	0 addr1 u1
	begin
		addr2 u2 search ( u addr3 u3 f ) \ Remaining text and flag on stack
		while
			\ Found string, increment counter and repeat
			rot 1+ -rot ( u+1 addr3 u3 ) \ Inc counter
			u2 safe/string  ( u addr4 u4 ) \ Remove match at beginning of rest of text
	repeat
	2drop ( u ) ;

: c>str ( c -- addr1 ) here tuck c! 1 ; \ Convert char to string (gets overwritten on next call!)
: prepend-comma ( addr1 u1 -- addr2 u2 ) s" ," 2swap s+ ;
: join-on-str ( addr1 u1 addr2 u2 addr3 u3 -- addr4 u4 )
	\ Join addr1 u1 and addr2 u2 on string addr3 u3
	2>r 2swap 2r> s+ 2swap s+ ;
: join-on-comma ( addr1 u1 addr2 u2 -- addr3 u3 ) s" ," join-on-str ;
: join-on-space ( addr1 u1 addr2 u2 -- addr3 u3 ) 32 c>str join-on-str ;

: str>u ( addr1 u1 -- u ) s>number? 2drop ; \ Unchecked str to unsigned int conversion
: u>str ( u1 -- addr2 u2 ) ['] u. >string-execute 1- ; \ Convert unsigned int to string

: zero-pad ( u1 -- addr1 u1 )
	dup u>str ( u1 addr1 u1 )
	rot 10 < if s" 0" 2swap s+ endif
;
: yyyy-mm-dd ( nyear nmonth nday -- addr1 u1 )
	u>str rot zero-pad s" -" join-on-str rot zero-pad s" -" join-on-str ;
: hh:mm ( nhour nmin -- addr1 u1 )
	zero-pad rot zero-pad s" :" join-on-str ;

: seconds-since-epoch ( -- u1 ) \ Return seconds since epoch
	utime #1000000 um/mod nip ;
: seconds>str ( u1 -- addr1 u1 )
	0 ( ud )
	>time&date&tz ( nsec nmin nhour nday nmonth nyear fdst ndstoff c-addrtz utz )
	2drop 2drop ( nsec nmin nhour nday nmonth nyear )
	\ Format YYYY-MM-DD part
	yyyy-mm-dd ( nsec nmin nhour addr1 u1 )
	2>r ( nsec nmin nhour )
	\ Format HH:MM part (skip the seconds)
	hh:mm ( nsec addr2 u2 )
	rot drop
	2r> ( addr2 u2 addr1 u1 )
	2swap join-on-space ( addr1 u1 )
;

: quote-if-needed ( addr1 u1 -- addr1 u1 )
	\ Performs CSV quoting if separator found in string
	2dup csv-separator scan nip 0<> ( addr1 u1 f )
	if
		['] .quoted-csv >string-execute
	endif ( addr1 u1 ) ;

\ CSV Writing

: create-statsfile ( -- wfileid )
	CSVFILE r/w create-file throw ( wfileid )
	s" filename,todocount,timestamp" third ( wfileid addr1 u1 wfileid )
	write-line throw ;

: reposition-to-end ( wfileid -- )
	\ Change position in fileid to EOF
	dup file-size throw ( wfileid ud )
	rot reposition-file throw ;

: open-csv-append ( -- wfileid )
	CSVFILE file-status ( wfam wior )
	case
		2dup 2 0 d= \ File exists
		?of 2drop CSVFILE r/w open-file throw ( wfileid )
		dup reposition-to-end endof

		0 -514 d= \ File doesn't exist
		?of create-statsfile ( wfileid ) endof

		\ Some other error
		s" Error when opening todostats.csv" type cr
		-517 throw \ Throw I/O error
	0 endcase ; \ 0 for endcase to drop (we don't reach it though)

: csvfileid ( -- wfileid )
	\ Returns the file id for the csv file (singleton)
	csvfd @ ( wfileid )
	dup 0= ( wfileid f )
	if \ Hasn't been initialized, create csv file and save fd to csvfd
		drop open-csv-append ( wfileid )
		dup csvfd ! ( wfileid ) \ Save fileid to csvfd variable
	endif ( wfileid ) ;

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is a filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;

: todos-to-csv {: addr1 u1 -- :}
	\ Count todo's in the file (addr1 u1) (excluding todostats.csv) and append the results to the CSV file
	addr1 u1 CSVFILE string-suffix? 0= ( addr1 u1 f ) \ Make sure this isn't the stats file, we skip that
	if
		addr1 u1
		\ Count number of todos, convert to string
		2dup todos-in-file u>str ( addr1 u1 addr2 u2 )
		\ Before we join, check if we need to quote filepath
		2swap quote-if-needed 2swap ( addr1 u1 addr2 u2 )
		\ Join todo count with comma to filename
		join-on-comma ( addr1 u1 )
		\ Get seconds since epoch, turn to string and join on comma
		seconds-since-epoch u>str join-on-comma ( addr1 u1 )
		\ Write to CSV file
		csvfileid write-line throw
	endif ;

\ CSV Reporting

$[]Variable statsarray

: locate-file-in-stats {: addr1 u1 -- u :}
	statsarray $[]# ( u ) \ Get size
	0 u+do
		i statsarray $[]@ ( addr2 u2 ) \ Get string at index i
		next-csv 2nip ( addr2 u2 ) \ Get only filename part of string
		addr1 u1 str= if i unloop exit endif \ Return i if filename matches
	loop
	\ Not found in array, add it
	\ But first note the size, this is the index we return
	statsarray $[]# ( u )
	addr1 u1 quote-if-needed ( u addr1 u1 ) \ Add CSV quotes to delimit
	statsarray $+[]! ( u ) \ Appended
;

variable curr-index

: gather-cell-data ( addr1 u1 u2 u3 )	
	1- 0= if drop 2drop exit endif \ Skip header line
	case \ Each of the columns
		0 of locate-file-in-stats curr-index ! endof \ Filename cell
		1 of
			prepend-comma curr-index @ ( addr1 u1 u )
			statsarray $[]+! \ Append to string
		endof \ todo-count
		2 of
			prepend-comma curr-index @ ( addr1 u1 u )
			statsarray $[]+! \ Append to string
		endof \ timestamp
	endcase
;

\ Per file stats:
\ Rate of TODOs being removed (+/- x TODOs per day/week)
\ Most busy period

variable last-timestamp
variable last-todocount
variable min-todo
variable min-timestamp
variable max-todo
variable max-timestamp
variable sum-todo
variable n-todo

variable max-delta
2variable delta-period

: reset-vars ( -- ) \ Reset variables used for statistics calculations
	0 last-timestamp ! 0 last-todocount !

	-1 min-todo ! 0 min-timestamp !
	0 max-todo ! 0 max-timestamp !

	0 sum-todo ! 0 n-todo !

	0 max-delta ! 0 0 delta-period 2!
;

: get-next-measurement ( addr1 u1 -- addr1 u1 u2 u3 )
	\ A measurement is a (todo-count, timestamp) pair
	\ Returns remaining string, todo-count and timestamp
	next-csv str>u -rot ( u2 addr1 u1 )
	next-csv str>u -rot ( u2 u3 addr1 u1 )
	2swap
;

: update-min-todo ( u1 -- f )
	\ Update min-todo if current todocount is smaller, return true if value changed
	dup min-todo @ tuck ( u1 u2 u1 u2 )
	u<= -rot ( f u1 u2 )
	umin min-todo !
;
: update-max-todo ( u1 -- f )
	\ Update max-todo if current todocount is greater return true if value changed
	dup max-todo @ tuck ( u1 u2 u1 u2 )
	u>= -rot ( f u1 u2 )
	umax max-todo !
;

: update-min/max {: u1 u2 -- :}
	\ Update min/max
	u1 update-min-todo if u2 min-timestamp ! endif
	u1 update-max-todo if u2 max-timestamp ! endif
;
: update-sum/count ( u1 -- )
	\ Update sum and count
	sum-todo +! 1 n-todo +! ;


: parse-measurement {: u1 u2 -- :}
	\ Takes in (u1=todocount u2=timestamp), updates variables and calculates stats
	u2 last-timestamp @ < if s" Timestamp decreased, please sort the csv" type cr bye endif

	u1 u2 update-min/max
	u1 update-sum/count

	\ Update max delta
	last-todocount @ u1 - ( u ) \ How much did it increase/decrease?
	\ Update vars if delta was greater and last-timestamp isn't 0
	dup abs max-delta @ abs >= ( f )
	last-timestamp @ 0> and ( f )
	if dup max-delta ! last-timestamp @ u2 delta-period 2! endif
	drop

	\ Save variables for next iteration
	u2 last-timestamp !
	u1 last-todocount !
;

: indent ( -- ) 4 spaces ;
: print ( addr1 u1 -- ) type space ;

: calculate-statistics ( addr1 u1 -- )
	reset-vars \ Reset variables used for parsing file stats
	begin
		dup 0> ( ... addr1 u1 f )
		while
			get-next-measurement ( addr1 u1 todocount timestamp )
			parse-measurement ( addr1 u1 )
	repeat
	2drop ( addr1 u1 ) \ Drop remaining string (empty)
;

: make-report ( addr1 u1 -- )
	\ Takes in a stats string and prints a report
	next-csv 2swap ( addr1 u1 addr2 u2 ) \ Remaining string is todo data
	calculate-statistics 
	min-todo @ 0= max-todo @ 0= and if exit endif \ Skip files that never had todos

	cr s" File:" print type cr
	indent s" Min. # of TODOs:" print
		min-todo @ u.
		4 spaces s" [" type min-timestamp @ seconds>str type s" ]" type cr
	indent s" Max. # of TODOs:" print
		max-todo @ u.
		4 spaces s" [" type max-timestamp @ seconds>str type s" ]" type cr

	\ Only print this info if the number of todos actually changes
	max-delta @ 0<> if
		indent s" Avg. # of TODOs:" print
			sum-todo @ s>f n-todo @ s>f f/ f. cr

		indent s" Largest change :" print
			max-delta @ .
		4 spaces s" [" type delta-period 2@ swap seconds>str print s" to" print seconds>str type s" ]" type cr
	endif

	cr
;
