
2variable statsfile
"todostats.csv" statsfile 2!

: discard-n-chars ( addr1 u1 n -- addr2 u2 )
	\ Increment addr1 by n and decrement u1 by n (discard beginning of string)
	tuck - ( addr1 n u2 )
	-rot + ( u2 addr2 )
	swap ;

: count-substr ( addr1 u1 addr2 u2 -- u3 ) recursive
	{ a1 u1 a2 u2 }
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	a1 u1 a2 u2 search ( a3 u3 f )
	if \ Found string, recurse on rest
		u2 ( a3 u3 u2 )
		discard-n-chars ( a4 u4 )
		a2 u2 ( a4 u4 a2 u2 )
		count-substr ( u )
		1+
	else
		2drop 0 ( 0 )
	endif ;

: TODOs-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is the filename, returns number of "TODO"s in the file
	slurp-file ( addr2 u2 )
	s" TODO"
	count-substr ( u3 ) ;
