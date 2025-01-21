
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

: join-on-comma ( addr1 u1 addr2 u2 -- addr3 u3 )
	\ Join 2 strings on a comma
	2swap s" ," s+ 2swap s+ ;

: seconds-since-epoch ( -- u1 ) \ Return seconds since epoch
	utime #1000000 um/mod nip ;

: u->str ( u1 -- addr2 u2 ) \ Convert unsigned number to string
	['] u. >string-execute 1- ;

: quote-if-needed ( addr1 u1 -- addr1 u1 )
	\ Performs CSV quoting if separator found in string
	2dup csv-separator scan nip 0<> ( addr1 u1 f )
	if
		['] .quoted-csv
		>string-execute
	endif ( addr1 u1 ) ;

\ CSV Writing

: create-statsfile ( -- wfileid )
	\ Create todostats.csv
	CSVFILE r/w create-file throw ( wfileid )
	\ Write CSV header
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
		drop open-csv-append ( wfileid )
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
		\ Count number of todos, convert to string
		2dup todos-in-file u->str ( addr1 u1 addr2 u2 )
		\ Before we join, check if we need to quote filepath
		2swap quote-if-needed 2swap ( addr1 u1 addr2 u2 )
		\ Join todo count with comma to filename
		join-on-comma ( addr1 u1 )
		\ Get seconds since epoch, turn to string and join on comma
		seconds-since-epoch u->str join-on-comma ( addr1 u1 )
		\ Write to CSV file
		csvfileid write-line throw
	endif ;

\ CSV Reporting

3 Constant BLOCKSIZE

variable stats \ Heap buffer (addr1,u1,addr2 blocks)
BLOCKSIZE 100 * 1+ cells allocate throw \ 100 file limit for now, we could increase this or expand dynamically TODO
stats !
\ Write size to stats
0 stats @ !

: file-count ( -- u ) stats @ @ ; \ Current number of files in stats
: as-blocks ( u -- u ) BLOCKSIZE * cells ;
: next-free-addr ( -- addr )
	stats @ cell+ ( addr )
	file-count as-blocks + ( addr )
;
: inc-file-count ( -- ) stats @ @ 1+ stats @ ! ;
: file-block-start ( -- addr ) stats @ cell+ ;
: block->filename ( addr -- addr1 u1 ) 2@ ;
: block->third ( addr -- addr2 ) cell+ cell+ @ ;

: add-file-to-stats ( addr1 u1 -- addr2 )
	next-free-addr ( ... addr )
	-rot third ( addr addr1 u1 addr )
	2! ( addr )
	dup cell+ cell+ 0 swap !
	inc-file-count
;
: find-file-in-stats {: addr1 u1 -- addr2 wior :}
	\ wior=-1 if file was found, then addr2 is address of its block, otherwise wior=0
	file-block-start ( addr2 )
	file-count ( addr2 u ) \ Files in stats buffer
	0 u+do  ( addr2 )
		dup 2@ ( addr2 addr3 u3 ) \ Get filename
		addr1 u1 str= if true unloop exit ( addr2 -1 ) endif \ Found address of file in stats
		BLOCKSIZE cells + \ Increment by block size cells
	loop ( addr2 )
	false ( addr2 0 )
;

: emplace-file-in-stats ( addr1 u1 -- addr2 )
	2dup find-file-in-stats
	if 2nip exit endif
	drop add-file-to-stats \ No address found, add file to stats
;

: print-stats ( -- )
	file-block-start
	file-count 0 u+do
		dup block->filename type space dup block->third . cr
		BLOCKSIZE cells +
	loop
	drop
;

create csvlinebuf 0 , 0 , 0 , 0 ,

: clear-linebuf ( -- )
	csvlinebuf 2 cells + @ ( u )
	csvlinebuf 2@ ( ... addr1 u1 )
	emplace-file-in-stats ( ... addr2 )
	2 cells + ( u addr3 )
	+!
;

: analyze-csv-cell ( addr1 u1 u2 u3 )	
	1- 0= if drop 2drop exit endif \ Skip header line
	case \ Each of the columns
		0 of csvlinebuf 2! endof \ Filename cell
		1 of s>number? 2drop csvlinebuf 2 cells + ! endof \ todo-count
		2 of s>number? 2drop csvlinebuf 3 cells + ! clear-linebuf endof \ timestamp
	endcase
;

