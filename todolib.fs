
\ Functions specific to counting to-do's (and helper functions)

s" todo.stats" 2Constant STATSFILE
variable statsfd

\ Helper functions

: count-substr {: addr1 u1 addr2 u2 -- u3 :}
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	0 addr1 u1
	case
		addr2 u2 search ( u addr3 u3 f ) \ Remaining text and flag on stack
		?of
			\ Found string, increment counter and repeat
			rot 1+ -rot ( u+1 addr3 u3 ) \ Inc counter
			u2 safe/string  ( u addr4 u4 ) \ Remove match at beginning of rest of text
		contof
	endcase   \ Drops one value
	drop ( u ) \ Drops one value
;

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

\ Statsfile handling

: create-statsfile ( -- wfileid )
	STATSFILE r/w create-file throw ( wfileid )
	s" filename,todocount,timestamp" third ( wfileid addr1 u1 wfileid )
	write-line throw ;

: reposition-to-end ( wfileid -- )
	\ Change position in fileid to EOF
	dup file-size throw ( wfileid ud )
	rot reposition-file throw ;

: open-statsfile ( -- wfileid )
	STATSFILE file-status ( wfam wior )
	case
		2dup 2 0 d= \ File exists
		?of 2drop STATSFILE r/w open-file throw ( wfileid )
			dup reposition-to-end ( wfileid ) endof

		0 -514 d= \ File doesn't exist
		?of create-statsfile ( wfileid ) endof

		\ Some other error
		s" Error when opening todostats.csv" type cr
		-517 throw \ Throw I/O error
	0 endcase ; \ 0 for endcase to drop (we don't reach it though)

: csvfileid ( -- wfileid )
	\ Returns the file id for the csv file (singleton)
	statsfd @ ( wfileid )
	dup 0= ( wfileid f )
	if \ Hasn't been initialized, create csv file and save fd to statsfd
		drop open-statsfile ( wfileid )
		dup statsfd ! ( wfileid ) \ Save fileid to statsfd variable
	endif ( wfileid ) ;

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is a filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;

: todos-to-csv {: addr1 u1 -- :}
	\ Count todo's in the file (addr1 u1) (excluding todostats.csv) and append the results to the CSV file
	addr1 u1 STATSFILE string-suffix? 0= ( addr1 u1 f ) \ Make sure this isn't the stats file, we skip that
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
		\ Write to stats file
		csvfileid write-line throw
	endif ;
