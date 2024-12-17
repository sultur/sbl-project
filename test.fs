
\ This is a test file
\ TODO: Add some tests
\ TODO: Remove this todo

:noname
	s" Here there is a TODO, and another TODO"
	s" TODO"
	count-substr
	assert( dup 2 = )
	drop ;
execute

: print cr type ;
s" code" ' print walk-dir