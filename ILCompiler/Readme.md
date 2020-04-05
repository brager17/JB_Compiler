Грамматика :
```
< program> ::=  <statement>

 <statement> ::= 
            | "if"  "(" expression ")" "{ <statement> }"
            | "if"  "(" expression ")" "{ <statement> }" "else" "{" <statement> "}"
            | [ {statement,";"} ] "return" <expression> ";"


 <assignement> ::=
            | <type_keyword> <variable> "=" <expression> ";"
            | <variable> "=" <expression> ";"

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

Первоначально я хотел генерировать код подобный компилятору Roslyn, поэтому параллельно с собственной компиляцией, я компилировал программу еще и Roslyn'ом, а потом, используя Mono.Cecil сравнивал содержимое. 

Это можно заметить в некоторых тестах, например:


![](https://habrastorage.org/webt/hf/ip/zj/hfipzj3ghfhocwcm9x89mh78bxm.png)


В итоге код для expression'ов в которых не участвуют числа, получился идентичным, но если выражении появляется операция над числами, то Roslyn сворачивает константы. Я поддержал свертку констант : 


![](https://habrastorage.org/webt/cr/jy/a-/crjya-vwazvnb1nm01dgnn-syss.png)

