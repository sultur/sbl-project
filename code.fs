
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