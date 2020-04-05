Грамматика :
```
< program> ::=  <statement>

 <statement> ::= 
            | "if"  "(" expression ")" "{ <statement> }"
            | "if"  "(" expression ")" "{ <statement> }" "else" "{" <statement> "}"
            | [ {statement,";"} ] "return" <expression> ";"

 <assignement> ::= <type_keyword> <variable> "=" <expression> ";"

 <method_call> = <letter> "(" <args> ")" 
 <args> ::= [<expression> { "," <expression> }]

 <expression> ::= 
            | <simple_expression> 
            | <simple_expression> <relational_operator> <simple_expression>
 
 <simple_expression> ::= 
            | <term> 
            | <sign> <term>
            | <simple expression> <addition_operator> <term>

 <term> ::= 
        | <factor> 
        | <term> <multiplying_operator> <factor>
 
 <factor> ::=  
        | <variable> 
        | <constant> 
        | (<expression>) 
        | "!" <factor>

 <variable> ::=  | <letter> { <letter> <digit> }

 <relational_operator> ::= "==" | "!=" | ">" | ">=" | "<" | "<=" 

 <addition_operator> ::= "+" | "-" | "||"

 <multiplying_operator> ::= "*" | "/" | "&&"
 
 <constant> ::= "true" | "false" | <number>

 <type_keyword> ::= <boolean> | "int" | "true"

 <boolean> ::= "true" | "false"
 <sign> ::="-"
 <number> ::= ["-"] digit {digit}
 <letter> ::= ("a" |...| "z") | ( "A" | ... | "Z") 
 <digit>  ::= "0" | "1" | "2" | "3" | "4" | "5" | "6" | "7" | "8" | "9" ;

```