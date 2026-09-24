# HTML

A high performance HTML parser for .NET.

## High-level API

This API builds a DOM tree from the HTML document.

```cs
var dom = HtmlDocument.Parse(html);
```

Explore the DOM:

```cs
var childNodes = dom.Nodes;
var children = dom.Elements();
var descendants = dom.Descendants();
```

Query the DOM:

```
var element = document.GetElementById("id");
var elements = document.GetElementsByTagName("div");
var elements = document.GetElementsByClassName("class");
var elements = document.QuerySelectorAll("div.class");
```

## Low-level lazy API

This API parses the HTML document on the fly, as you explore the DOM tree.

```cs
var dom = LazyHtmlDocument.Parse(html); // no parsing is done yet
```

Explore the DOM:

```cs
var childNodes = dom.Nodes();
var children = dom.Elements();
var descendants = dom.Descendants();
```

Query the DOM:

```
var element = document.GetElementById("id");
var elements = document.GetElementsByTagName("div");
var elements = document.GetElementsByClassName("class");
var elements = document.QuerySelectorAll(
	element: "div",
	classNames: "class",
	attributes: [new("data-attr", "value")]
);
```

## Writer

High performance HTML writer.

```cs
var writer = HtmlWriter.Create(new ArrayBufferWriter<char>());

writer.WriteHtml5Doctype();
writer.OpenElement("p");
writer.WriteText("click");
writer.OpenElement("a");
writer.WriteAttribute("href", "https://example.com");
writer.WriteText("here");
writer.CloseElement();
writer.CloseElement();
```