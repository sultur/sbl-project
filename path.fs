
\ Utility functions for dealing with paths to files and directories

1024 constant MAX-PATH-LENGTH ( We run into trouble if the filepaths are longer than 1024 chars )

MAX-PATH-LENGTH buffer: filename-buffer ( Buffer which we re-use for read-dir-str )

: hidden? ( addr1 u1 -- f ) s" .*" filename-match ; \ Return -1 if filename starts with "."

: special-dir? ( addr1 u1 -- f )
	\ Return true if filename specified by addr1 u1 is either "." or ".."
	\ TODO: compare to basename
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

: read-dir-str {: dirid -- addr1 u1 wior :}
	\ Return next entry in directory as a string.
	\ If an error occurs then wior<>0, if no entries are left then wior = u1 = 0
	\ NOTE:
    \ - The string lives only until next invocation (gets overwritten)
	\ - The directory hardlinks "." and ".." are skipped
	begin
		\ Read next entry from dirid into filename-buffer
		filename-buffer MAX-PATH-LENGTH dirid read-dir ( u2 flag wior )
		\ Check whether this is "." or ".."
		third filename-buffer swap ( u2 flag wior addr1 u2 )
		special-dir? dup           ( u2 flag wior f f )
		if \ Loop back if this entry is "." or ".."
			2nip nip \ Clear stuff on stack before looping back
		endif
		( f )
		invert \ Invert the special dir flag for 'until'
	until
	nip over ( u2 wior u2 )
	0> filename-buffer and ( u2 wior addr1 ) \ if no entries left bitwise u2 & addr=0
	-rot ( addr1 u2 wior ) ;

: concat-path {: addr1 u1 addr2 u2 -- addr3 u3 :}
	\ Join two paths on a slash
	addr1 u1 s" /" s+
	addr2 u2 s+
;

: walk-dir {: addr1 u1 xt :} recursive
	\ Recursively find entries in directory and execute xt for each
	\ xt should have signature ( addr1 u1 -- ) where addr1 u1 represents the path
	addr1 u1 open-dir throw ( wdirid )
	begin                   ( wdirid )
		dup read-dir-str throw ( wdirid addr2 u2 )
		dup 0=                ( wdirid addr2 u2 f ) \ Check if we got an empty entry
		if
			\ Finished with dir
			2drop close-dir throw
			exit
		endif
		addr1 u1 2swap concat-path ( wdirid addr3 u3 ) \ Get full relative path
		2dup dir? ( wdirid addr3 u3 f )
		if  \ Directory
			( wdirid addr3 u3 )
			xt walk-dir ( wdirid )
		else \ Not a directory (file/symlink)
			( wdirid addr3 u3 )
			xt execute ( wdirid )
		endif
	again
	close-dir throw ;