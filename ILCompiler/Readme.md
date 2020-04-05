### Грамматика :
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
            | <method_call>
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
<br>

### IlCompiler

Я хотел генерировать код подобный компилятору Roslyn, поэтому параллельно с собственной компиляцией, компилировал программу еще и Roslyn'ом, а потом, используя Mono.Cecil сравнивал содержимое. 

Это можно заметить в некоторых тестах, например:


![](https://habrastorage.org/webt/hf/ip/zj/hfipzj3ghfhocwcm9x89mh78bxm.png)


В итоге код для expression'ов в которых не участвуют числа, получился идентичным, но если выражении появляется операция над числами, то Roslyn сворачивает константы. Я поддержал свертку констант : 

![](https://habrastorage.org/webt/cr/jy/a-/crjya-vwazvnb1nm01dgnn-syss.png)


Точка входа в приложение - проект Starter, вы можете использовать статический метод класса ```Program``` ```Compile```. 
Компилятор может обрабатывать Statement и делает это отдельно используя методы ```Compiler.CompileStatement``` и ```Compiler.CompileExpression```.
 Есть перегруженные версии этих методов возвращающие сгенерированные IL инструкции. Это нужно было для тестирования.
 
Сам компилятор разделен на три части: ```Lexer```,```Parser``` и ```ILCompiler```. Lexer получает на вход строку и разбивает на токены, парсер же получает не только токены, но еще и контекст метода (нужно, чтобы обращаться к статическим полям и методам). Далее после парсинга ```ILCompiler``` получает только AST. ```ILCompiler'a```реализован как визитор, в базовом классе реализована логика обхода дерева, в наследнике работа с IlGenerator'ом. Но также не напрямую, а через посредника IlEmitter'a, это класс прослойка, позволяющий добавлять логгирование IL инструкций. 

Компилятор умеет делать все, что было дано в основном и всех дополнительных заданиях. Особенно интересно было реализовывать операторы && и ||  (short-circuiting операторы):

![](https://habrastorage.org/webt/zi/2b/xq/zi2bxqx46lkimcuvg_nw6ovbwsg.png)

Для тестирования этих операторов я добавил поддержку ref.
