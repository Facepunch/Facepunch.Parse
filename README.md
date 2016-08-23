# Facepunch.Parse
Context-free grammar parsing tool

[![facepunchprivate MyGet Build Status](https://www.myget.org/BuildSource/Badge/facepunchprivate?identifier=9dfcc07e-1ef6-45ad-b503-efaf91a3dda3)](https://www.myget.org/)

## Example
### Parser

```csharp
var whitespace = (Parser) new Regex(@"\s+|//[^\n]*\n|/\*(\*[^/]|[^\*])*\*/");
var root = new NamedParser("Root");
var decl = new NamedParser("Declaration");
var usng = new NamedParser("Using");
var ident = new NamedParser("Identifier");
var qualident = new NamedParser("QualifiedIdentifier");

using (ConcatParser.ForbidWhitespace())
{
    ident.Resolve(new Regex("[a-z_][a-z0-9_]*", RegexOptions.Compiled | RegexOptions.IgnoreCase));

    using (ConcatParser.AllowWhitespace(whitespace))
    {
        qualident.Resolve(ident | (ident + "." + qualident));

        root.Resolve(decl | (decl + root));
        decl.Resolve(usng);
        usng.Resolve("using" + qualident + ";");
    }
}

var result = root.Parse(File.ReadAllText(args[0]));
Console.WriteLine(result.ToXElement());
```

### Input

```
using System;
//using System.Reflection;
using System.Reflection.Emit;

/*

using Foo.Bar;

*/

using System.Collections.Generic;

using something invalid!

```

### Output

```xml
<Root>
  <Declaration>
    <QualifiedIdentifier>
      <Identifier>System</Identifier>
    </QualifiedIdentifier>
  </Declaration>
  <Declaration>
    <QualifiedIdentifier>
      <Identifier>System</Identifier>
      <Identifier>Reflection</Identifier>
      <Identifier>Emit</Identifier>
    </QualifiedIdentifier>
  </Declaration>
  <Declaration>
    <QualifiedIdentifier>
      <Identifier>System</Identifier>
      <Identifier>Collections</Identifier>
      <Identifier>Generic</Identifier>
    </QualifiedIdentifier>
  </Declaration>
  <Declaration>
    <QualifiedIdentifier>
      <Identifier>something</Identifier>
      <Token>
        <ParseError>Expected '.' at (13, 17)</ParseError>
      </Token>
    </QualifiedIdentifier>
  </Declaration>
</Root>
```
