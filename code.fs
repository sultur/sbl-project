
\ TODO: Make this non-recursive
: count-substr {: addr1 u1 addr2 u2 -- u3 :} recursive
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	addr1 u1 addr2 u2 search ( a3 u3 f )
	if                               \ Found string, recurse on rest
		u2 safe/string ( a4 u4 )     \ Remove match at beginning of rest of text
		addr2 u2 count-substr ( u )  \ How many times a2 u2 occurs in rest of text
		1+                           \ Plus the one we found first
	else
		2drop 0 ( 0 )       \ String occurs zero times in text
	endif ;

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is a filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;


\ TODO: Statistics reporting from csv file
\ TODO: use getenv to set csv file name?