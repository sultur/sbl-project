
\ Functions specific to counting to-do's

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

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is a filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;


\ TODO: Statistics reporting from csv file
\ TODO: use getenv to set csv file name?

: append-to-csv ( addr1 u1 u2 -- )
     \ Append filename (addr1 u1) and TODO count (u2) to the CSV file
    s" todostats.csv" r/o open-file
        if
            \ File does not exist
            s" todostats.csv" r/w create-file throw >r
            s" Filename,TODO Count,timestamp" r@ write-line throw
        else
            drop
            s" todostats.csv" r/w open-file throw >r
            r@ file-size throw r@ reposition-file throw
            0
        then

        \ Prepare the TODO count as a string
        base @ >r decimal
        <# #s #> r> base !

        \ Write the filename and TODO count to the CSV file
        2swap
        r@ write-file throw 
        s" ," r@ write-file throw

        \ Write the second string (addr2 u2)
        r@ write-file throw

        s" ," r@ write-file throw

        utime 
        base @ >r decimal
        <# # #s #> r> base !
        r@ write-file throw
        
        s" " r@ write-line throw
        r> close-file throw ;

: todos-to-csv ( addr1 u1 -- )
    \ Process the file (addr1 u1) and append the results to the CSV file
    2dup todos-in-file \ Get the count of TODOs
    append-to-csv ;