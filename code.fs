
1024 constant MAX-PATH-LENGTH ( We run into trouble if the filepaths are longer than 1024 chars )

MAX-PATH-LENGTH buffer: filename-buffer ( Buffer which we re-use for read-dir )

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

: read-dir-str {: dirid -- addr1 u1 wior :}
	\ Return next entry in directory as a string.
	\ If an error occurs or no entries are left wior<>0
	\ NOTE: The string lives only until next invocation (gets overwritten)
	filename-buffer MAX-PATH-LENGTH dirid ( addr1 u1 dirid )
	read-dir ( u2 flag wior )    \ Reads next entry from dir into filename-buffer
	swap invert or  ( u2 wior2 ) \ Return <>0 when either wior<>0 or wior=flag=0
	filename-buffer -rot ( addr1 u2 wior ) ;

: hidden? ( addr1 u1 -- f ) s" .*" filename-match ; \ Return -1 if filename starts with "."
: special-dir? ( addr1 u1 -- f )
	\ Return true if filename specified by addr1 u1 is either "." or ".."
	2dup s" ." filename-match ( f f )
	-rot s" .." filename-match ( f f )
	or ( f ) ;
: dir? ( addr1 u1 -- f )
	\ Return -1 if string specified by addr1 u1 is the name of a directory
	\ TODO: Handle special-dirs
	open-dir ( wdirid wior )
	0= ( wdirid f )
	if \ open-dir succeeded, it is a directory, close it again
		close-dir throw -1
	else \ Not a dir, return 0
		drop 0
	endif
;

\ TODO: Bug due to not having full file paths
: walk-dir {: addr1 u1 xt :} recursive
	\ Recursively find entries in directory and execute xt for each
	\ xt should have signature ( addr1 u1 -- ) where addr1 u1 represents the path
	addr1 u1 open-dir throw ( wdirid )
	begin
		dup read-dir-str -rot ( wdirid wior addr1 u1 )
		cr ...
		2dup addr1 u1 s" /" s+ 2swap s+ dir? ( wdirid wior addr1 u1 f )           \ Is this a directory?
		if
			2dup special-dir? 0= \ Ignore dirs . and ..
			if
				cr s" recursing" type
				cr ...
				xt walk-dir \ Recurse on this directory
			endif
		else \ File (or symlink?)
			cr ...
			xt execute ( wdirid wior )
		endif
	until
	close-dir throw ;

\ TODO: Statistics reporting from csv file
\ TODO: use getenv to set csv file name?