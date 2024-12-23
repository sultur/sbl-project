
512 constant MAX-FSNAME-LENGTH ( We run into trouble if the filenames are longer than 512 chars )

MAX-FSNAME-LENGTH buffer: filename-buffer ( Buffer which we re-use for read-dir )

: discard-n-chars ( addr1 u1 n -- addr2 u2 )
	\ Increment addr1 by n and decrement u1 by n (discard beginning of string)
	tuck - ( addr1 n u2 ) -rot + ( u2 addr2 ) swap ;

: count-substr ( addr1 u1 addr2 u2 -- u3 ) recursive
	{ a1 u1 a2 u2 }
	\ Counts how often string specified by addr2 u2 occurs
	\ in string specified by addr1 u1
	a1 u1 a2 u2 search ( a3 u3 f )
	if \ Found string, recurse on rest
		u2 discard-n-chars ( a4 u4 ) \ Remove match at beginning of rest of text
		a2 u2 count-substr \ How many times a2 u2 occurs in rest of text
		1+ ( u ) \ Plus the one we found first
	else
		2drop 0 ( 0 ) \ String occurs zero times in text
	endif ;

: todos-in-file ( addr1 u1 -- u2 )
	\ addr1 u1 is the filename, returns number of "TODO"s in the file
	slurp-file s" TODO" count-substr ;

: read-dir-easy ( dirid -- addr1 u1 ior )
	;

\ TODO: Recursively search files in given directory
: exec-on-files-in-dir ( addr1 u1 xt -- )
	\ use execute to run xt
	-rot ( xt addr1 u1 )
	open-dir throw ( xt wdirid )

;

\ TODO: Statistics reporting from csv file