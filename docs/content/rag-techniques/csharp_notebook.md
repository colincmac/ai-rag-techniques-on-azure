---
title: 
layout: page
tags: [post, notebook]
---
# Test CSharp Notebook


```csharp
#i "nuget:https://api.nuget.org/v3/index.json" 
#i "nuget:https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json" 
#i "nuget:https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" 

#r "nuget:Microsoft.Data.Analysis, 0.21.0"
#r "nuget: Plotly.NET.Interactive, 4.2.0"
#r "nuget: Plotly.Net, 4.2.0"

using Microsoft.Data.Analysis;
```


<p>
<details>
<summary>Example Output</summary>
<div><div><strong>Restore sources</strong><ul><li><span>https://api.nuget.org/v3/index.json</span></li><li><span>https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json</span></li><li><span>https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json</span></li></ul></div><div></div><div><strong>Installed Packages</strong><ul><li><span>Microsoft.Data.Analysis, 0.21.0</span></li><li><span>Plotly.Net, 4.2.0</span></li><li><span>Plotly.NET.Interactive, 4.2.0</span></li></ul></div></div>
</details>
</p>







```csharp
DateTimeDataFrameColumn dateTimes = new DateTimeDataFrameColumn("DateTimes"); // Default length is 0.
Int32DataFrameColumn ints = new Int32DataFrameColumn("Ints", 6); // Makes a column of length 3. Filled with nulls initially
StringDataFrameColumn strings = new StringDataFrameColumn("Strings", 6); // Makes a column of length 3. Filled with nulls initially

```


```csharp
// Append 6 values to dateTimes
dateTimes.Append(DateTime.Parse("2019/01/01"));
dateTimes.Append(DateTime.Parse("2019/01/01"));
dateTimes.Append(DateTime.Parse("2019/01/02"));
dateTimes.Append(DateTime.Parse("2019/02/02"));
dateTimes.Append(DateTime.Parse("2019/02/02"));
dateTimes.Append(DateTime.Parse("2019/03/02"));
```


```csharp
DataFrame df = new DataFrame(dateTimes, ints, strings ); // This will throw if the columns are of different lengths
df["Ints"].FillNulls(100, inPlace: true);
df["Strings"].FillNulls("Bar", inPlace: true);
```


```csharp
df
```


<p>
<details>
<summary>Example Output</summary>
<table id="table_638605386676789272"><thead><tr><th><i>index</i></th><th>DateTimes</th><th>Ints</th><th>Strings</th></tr></thead><tbody><tr><td><i><div class="dni-plaintext"><pre>0</pre></div></i></td><td><span>2019-01-01 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr><tr><td><i><div class="dni-plaintext"><pre>1</pre></div></i></td><td><span>2019-01-01 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr><tr><td><i><div class="dni-plaintext"><pre>2</pre></div></i></td><td><span>2019-01-02 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr><tr><td><i><div class="dni-plaintext"><pre>3</pre></div></i></td><td><span>2019-02-02 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr><tr><td><i><div class="dni-plaintext"><pre>4</pre></div></i></td><td><span>2019-02-02 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr><tr><td><i><div class="dni-plaintext"><pre>5</pre></div></i></td><td><span>2019-03-02 00:00:00Z</span></td><td><div class="dni-plaintext"><pre>100</pre></div></td><td>Bar</td></tr></tbody></table><style>
.dni-code-hint {
    font-style: italic;
    overflow: hidden;
    white-space: nowrap;
}
.dni-treeview {
    white-space: nowrap;
}
.dni-treeview td {
    vertical-align: top;
    text-align: start;
}
details.dni-treeview {
    padding-left: 1em;
}
table td {
    text-align: start;
}
table tr { 
    vertical-align: top; 
    margin: 0em 0px;
}
table tr td pre 
{ 
    vertical-align: top !important; 
    margin: 0em 0px !important;
} 
table th {
    text-align: start;
}
</style>
</details>
</p>

