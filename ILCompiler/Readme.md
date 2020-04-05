### Demo
```csharp

        [UsedImplicitly]
        static void PrintErrorMessage()
        {
            Console.WriteLine(
                "Пожалуйста, передайте в качестве параметра z  число от 0 до 3, или заполните поле \"operation\"");
        }

        [UsedImplicitly]
        private static int operation = -1;

        [UsedImplicitly]
        private static bool useSecretOperation;

        [UsedImplicitly]
        private static long SecretOperation(long a, long b) => (a + b) << 1;

        static void Main(string[] args)
        {
            var calculator = @"
        int op = -1;
        if ((z < 0 || z > 3) && (operation < 0 || operation > 3) && !useSecretOperation)
        {
            PrintErrorMessage();
            return -1;
        }
        if(!(z < 0 || z > 3))
        { 
            op = z;
        }
        else
        {
            if(!(operation < 0 || operation > 3))
            {
               op = operation;
            }
        }
        
        if(op == 0) 
        {
            return x + y;
        }
        if(op == 1)
        {
            return x - y;
        }
        if(op == 2) 
        {
            return x * y;
        }
        if(op == 3)
        {
            return x / y;
        }
        return SecretOperation(x,y);
";

            var func = Compile(calculator);

            Console.WriteLine($"10 + 5 = {func(10, 5, 0)}"); // 10 + 5 = 15
            Console.WriteLine($"10 - 5 = {func(10, 5, 1)}"); // 10 - 5 = 5
            Console.WriteLine($"10 * 5 = {func(10, 5, 2)}"); // 10 * 5 = 50
            Console.WriteLine($"10 / 5 = {func(10, 5, 3)}"); // 10 / 5 = 2
            
            operation = 0;
            Console.WriteLine($"10 + 5 = {func(10, 5, 4)}"); // 10 + 5 = 15
            
            operation = -1;
            useSecretOperation = true;
            Console.WriteLine($"Secret Operation = {func(10, 5, 5)}"); // Secret Operation = 30

        }


```

### Грамматика :
<br>

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
<br>
Это можно заметить в некоторых тестах, например:
<br>

![](https://habrastorage.org/webt/hf/ip/zj/hfipzj3ghfhocwcm9x89mh78bxm.png)

<br>
В итоге код для expression'ов в которых не участвуют числа, получился идентичным, но если выражении появляется операция над числами, то Roslyn сворачивает константы. Я поддержал свертку констант : 
<br>
![](https://habrastorage.org/webt/cr/jy/a-/crjya-vwazvnb1nm01dgnn-syss.png)

<br>
Точка входа в приложение - проект Starter, вы можете использовать статический метод класса ```Program``` ```Compile```. 
Компилятор может обрабатывать Statement и делает это отдельно используя методы ```Compiler.CompileStatement``` и ```Compiler.CompileExpression```.
 Есть перегруженные версии этих методов возвращающие сгенерированные IL инструкции. Это нужно было для тестирования.
 <br>
Сам компилятор разделен на три части: ```Lexer```,```Parser``` и ```ILCompiler```. Lexer получает на вход строку и разбивает на токены, парсер же получает не только токены, но еще и контекст метода (нужно, чтобы обращаться к статическим полям и методам). Далее после парсинга ```ILCompiler``` получает только AST. ```ILCompiler'a```реализован как визитор, в базовом классе реализована логика обхода дерева, в наследнике работа с IlGenerator'ом. Но также не напрямую, а через посредника IlEmitter'a, это класс прослойка, позволяющий добавлять логгирование IL инструкций. 
<br>
Компилятор умеет делать все, что было дано в основном и всех дополнительных заданиях. Особенно интересно было реализовывать операторы && и ||  (short-circuiting операторы):
<br>
![](https://habrastorage.org/webt/zi/2b/xq/zi2bxqx46lkimcuvg_nw6ovbwsg.png)
<br>
Для тестирования этих операторов я добавил поддержку ref.
