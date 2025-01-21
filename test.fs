
\ TODO: Add some more tests

\ count-substr test
: test-count-substr
	s" Here there is a TODO, and another TODO"
	s" TODO"
	count-substr assert( dup 2 = ) drop
	s" nothing here" s" TODO" count-substr assert( dup 0= ) drop
;
test-count-substr

s" All tests passed!" type cr
bye
