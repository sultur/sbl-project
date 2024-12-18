
\ TODO: Add some more tests

:noname
	s" Here there is a TODO, and another TODO"
	s" TODO"
	count-substr
	assert( dup 2 = )
	drop ;
execute


: print-todos ( addr1 u1 -- )
	\ Print filepath along with the number of TODOs in the file
	2dup todos-in-file -rot ( u2 addr1 u1 )
	tuck type ( u2 u1 )
	25 swap - .r     \ Print TODO number aligned at col 25 (subtract path len)
	cr
;
s" code" ' print-todos walk-dir
