
\ Functions specific to counting to-do's (and helper functions)

\ Some helper functions

: count-substr {: addr1 u1 addr2 u2 -- u3 :}
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	0 addr1 u1 addr2 u2
	( u addr1 u1 addr2 u2 ) case
		search ( u a3 u3 f )
		0=
		?of \ No more occurrences
			2drop ( u )
		endof
		( u a3 u3 ) \ Found string, increment counter and repeat
		rot 1+ -rot ( u+1 a3 u3 ) \ Inc counter
		u2 safe/string  ( u+1 a4 u4 ) \ Remove match at beginning of rest of text
		addr2 u2  ( u+1 a4 u4 a2 u2 ) \ Find next matching substring in remaining of text
	next-case ;

: join-on-comma ( addr1 u1 addr2 u2 -- addr3 u3 )
	\ Join 2 strings on a comma
	2swap s" ," s+ 2swap s+ ;

: seconds-since-epoch ( -- u1 ) \ Return seconds since epoch
	utime #1000000 um/mod nip ;

: u->str ( u1 -- addr2 u2 ) \ Convert unsigned number to string
	['] u. >string-execute 1- ;

\ ------------
\ CSV handling

s" todostats.csv" 2Constant CSVFILE
variable csvfd

: create-statsfile ( -- wfileid )
	\ Create todostats.csv
	CSVFILE r/w create-file throw ( wfileid )
	\ Write CSV header
	s" filename,todocount,timestamp" third ( wfileid addr1 u1 wfileid )
	write-line throw ( wfileid )
;

: reposition-to-end ( wfileid -- )
	\ Change position in fileid to EOF
	dup file-size throw ( wfileid ud )
	rot reposition-file throw ( )
;

: open-statsfile ( -- wfileid )
	CSVFILE file-status ( wfam wior )
	case
		2dup 2 0 d= \ File exists
		?of 2drop CSVFILE r/w open-file throw ( wfileid )
			dup reposition-to-end ( wfileid ) endof

		2dup 0 -514 d= \ File doesn't exist
		?of 2drop create-statsfile ( wfileid ) endof

		\ Some other error
		s" Error when opening todostats.csv" type cr
		-517 throw \ Throw I/O error
	0 endcase ; \ 0 for endcase to drop (we don't reach it though)

: csvfileid ( -- wfileid )
	\ Returns the file id for the csv file (singleton)
	csvfd @ ( wfileid )
	dup 0= ( wfileid f )
	if \ Hasn't been initialized, create csv file and save fd to csvfd
		drop open-statsfile ( wfileid )
		dup csvfd ! ( wfileid ) \ Save fileid to csvfd variable
	endif ( wfileid ) ;

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is a filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;

: todos-to-csv {: addr1 u1 -- :}
	\ Count todo's in the file (addr1 u1) (excluding todostats.csv) and append the results to the CSV file
	addr1 u1 CSVFILE string-suffix? 0= ( addr1 u1 f ) \ Make sure this isn't the CSV file, we skip that
	if 
		addr1 u1
		\ Count number of todos, convert to string, join with comma to filename 
		2dup todos-in-file u->str join-on-comma ( addr1 u1 )
		\ Get seconds since epoch, turn to string and join on comma
		seconds-since-epoch u->str join-on-comma ( addr1 u1 )
		\ Write to CSV file
		csvfileid write-line throw
	endif ;
