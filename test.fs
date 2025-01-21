
\ TODO: Add some more tests

\ count-substr test
: test-count-substr
	s" Here there is a TODO, and another TODO"
	s" TODO"
	count-substr
	assert( dup 2 = )
	drop ;
test-count-substr

