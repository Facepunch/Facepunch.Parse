# Facepunch.Parse
Context-free grammar parsing tool

[![facepunchprivate MyGet Build Status](https://www.myget.org/BuildSource/Badge/facepunchprivate?identifier=9dfcc07e-1ef6-45ad-b503-efaf91a3dda3)](https://www.myget.org/)

## Example
### Parser

```csharp
var grammar = GrammarBuilder.FromString( @"
    Whitespace = /\s+/;
    Word = /[a-z]+/i;
    Period = '.' | '?' | '!';
    EndOfInput = /$/;

    ignore Whitespace
    {
        Document = Sentence (Document | EndOfInput);
        Sentence = Word (Sentence | Period);
    }
" );

var result = grammar["Document"].Parse(File.ReadAllText(args[0]));

Console.WriteLine(result.ToXElement());
```

### Input

```
Hello world! How are you? Testing-testing testing testing.
```

### Output

```
Error: Expected Word, '.', '?', or '!' at line 1, column 34
```

```xml
<Document index="0" length="33">
  <Sentence index="0" length="13">
    <Word index="0" length="5">Hello</Word>
    <Word index="6" length="5">world</Word>
    <Period index="11" length="1">!</Period>
  </Sentence>
  <Sentence index="13" length="13">
    <Word index="13" length="3">How</Word>
    <Word index="17" length="3">are</Word>
    <Word index="21" length="3">you</Word>
    <Period index="24" length="1">?</Period>
  </Sentence>
  <Sentence index="26" length="7">
    <Word index="26" length="7">Testing</Word>
    <Word index="33" length="0">
      <ParseError>Expected Word</ParseError>
    </Word>
    <Period index="33" length="0">
      <Token index="33" length="0">
        <ParseError>Expected '.'</ParseError>
      </Token>
      <Token index="33" length="0">
        <ParseError>Expected '?'</ParseError>
      </Token>
      <Token index="33" length="0">
        <ParseError>Expected '!'</ParseError>
      </Token>
    </Period>
  </Sentence>
</Document>
```
