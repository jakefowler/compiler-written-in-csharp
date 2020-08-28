program mathTest;

var
	val1, val2 : int;
	astring : string;
	
begin
	write ("Beginning Program");
	val1 := 1 + 2 + 3 + 4;
	write ("val1 = 1 + 2 + 3 + 4");
	write (val1);

	val2 := 3 * 4;
	write ("val2 = 3 * 4");
	write (val2);

	val1 := 20 / 5;
	write ("val1 = 20 / 5");
	write (val1);

	val2 := 6 - 7 * (8+9);
	write ("val2 = 6 - 7 * (8+9)");
	write (val2);

	val1 := (10 * (11 + 12) / 13 + (14 * 15));
	write ("val1 = (10 * (11 + 12) / 13 + (14 * 15))");
	write (val1);

	astring := "this string was assigned to variable astring";
	write(astring);

	val1 := 5-3;
	write ("val1 = 5-3");
	write (val1);

	val2 := val1 * val1 / (7 - 6);
	write ("val2 := val1 * val1 / (7 - 6)");
	write (val2);

	write ("Please enter a number to multiply by 10");
	read(val1);
	val1 := val1 * 10;
	write (val1)

end.