
\ Utility functions for dealing with paths to files and directories

1024 constant MAX-PATH-LENGTH ( We run into trouble if the filepaths are longer than 1024 chars )

MAX-PATH-LENGTH buffer: filename-buffer ( Buffer which we re-use for read-dir )

: read-dir-str {: dirid -- addr1 u1 wior :}
	\ Return next entry in directory as a string.
	\ If an error occurs then wior<>0, if no entries are left then wior = u1 = 0
	\ NOTE: The string lives only until next invocation (gets overwritten)
	filename-buffer MAX-PATH-LENGTH dirid ( addr1 u1 dirid )
	read-dir ( u2 flag wior )    \ Reads next entry from dir into filename-buffer
	swap invert or  ( u2 wior2 ) \ Return <>0 when either wior<>0 or wior=flag=0
	filename-buffer -rot ( addr1 u2 wior ) ;

: hidden? ( addr1 u1 -- f ) s" .*" filename-match ; \ Return -1 if filename starts with "."

: path-basename ( addr1 u1 -- addr2 u2 )
	\ Return the last portion of a filesystem path
	\ e.g. "long/path/base" -> "base"
	\ TODO
;

: special-dir? ( addr1 u1 -- f )
	\ Return true if filename specified by addr1 u1 is either "." or ".."
	\ TODO: compare to path-basename
	2dup s" ." str= ( addr1 u1 f )
	-rot s" .." str= ( f f )
	or ;

: dir? ( addr1 u1 -- f )
	\ Return -1 if string specified by addr1 u1 is the name of a directory
	2dup special-dir?  ( addr1 u1 f )
	if                    \ Either "." or ".."
		2drop -1 exit
	endif
	open-dir 0= ( wdirid f )
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