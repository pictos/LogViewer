using LogViewer.Parsers;
using NUnit.Framework;
using System.Collections.Immutable;
#if WINDOWS
#endif

namespace LogViewer.DeviceTests;

[TestFixture]
public class QueryTests
{
	static string LogFilePath { get; } = Path.Combine(BasePath, "app_lorem_ipsum.txt");

	static void AssertSameLines(ImmutableArray<LogInfo> expected, ImmutableArray<LogInfo> actual)
	{
		Assert.That(actual.Length, Is.EqualTo(expected.Length));
		for (var i = 0; i < expected.Length; i++)
			Assert.That(actual[i].Text, Is.EqualTo(expected[i].Text), $"Mismatch at index {i}");
	}

	[Test]
	public void Parse_RespectsOperatorPrecedence()
	{
		var node = QueryParser.Parse("info | debug & maui");

		Assert.That(node, Is.TypeOf<OrNode>());
		var or = (OrNode)node;
		Assert.That(or.Left, Is.TypeOf<TermNode>());
		Assert.That(or.Right, Is.TypeOf<AndNode>());

		var and = (AndNode)or.Right;
		Assert.That(and.Left, Is.TypeOf<TermNode>());
		Assert.That(and.Right, Is.TypeOf<TermNode>());
	}

	[Test]
	public void Parse_ParenthesesOverridePrecedence()
	{
		var node = QueryParser.Parse("(info | debug) & maui");

		Assert.That(node, Is.TypeOf<AndNode>());
		var and = (AndNode)node;
		Assert.That(and.Left, Is.TypeOf<OrNode>());
		Assert.That(and.Right, Is.TypeOf<TermNode>());
	}

	[Test]
	public void Parse_QuotedTermCreatesSingleLiteral()
	{
		var node = QueryParser.Parse("\"user logged in\"");

		Assert.That(node, Is.TypeOf<TermNode>());
		Assert.That(node.Key, Is.EqualTo("user logged in"));
	}

	[TestCase("info &")]
	[TestCase("(info | debug")]
	[TestCase("!")]
	[TestCase("\"")]
	[TestCase("\"\"")]
	public void Parse_InvalidExpression_Throws(string query) =>
		Assert.Throws<FormatException>(() => QueryParser.Parse(query));

	[Test]
	public void Parse_EmptyExpression_ThrowsArgumentException() =>
		Assert.Throws<ArgumentException>(() => QueryParser.Parse(string.Empty));

	[Test]
	public void Filter_PrecedenceMatchesExplicitGrouping()
	{
		using var reader = new LoggerReader(LogFilePath);
		reader.Process();

		var implicitPrecedence = reader.Filter("info | debug & maui");
		var explicitPrecedence = reader.Filter("info | (debug & maui)");

		AssertSameLines(explicitPrecedence, implicitPrecedence);
	}

	[Test]
	public void Filter_QueryNodeAndStringPathAreEquivalent()
	{
		using var reader = new LoggerReader(LogFilePath);
		reader.Process();

		const string query = "(info | debug) & !close";
		var fromString = reader.Filter(query);
		var fromNode = reader.Filter(QueryParser.Parse(query));

		AssertSameLines(fromString, fromNode);
	}

	[Test]
	public void Filter_AndOrNotFollowSetSemantics()
	{
		using var reader = new LoggerReader(LogFilePath);

		var all = reader.Process();
		var info = reader.Filter("info");
		var debug = reader.Filter("debug");
		var andResult = reader.Filter("info & debug");
		var orResult = reader.Filter("info | debug");
		var notInfo = reader.Filter("!info");

		Assert.That(andResult.Length, Is.LessThanOrEqualTo(info.Length));
		Assert.That(andResult.Length, Is.LessThanOrEqualTo(debug.Length));
		Assert.That(orResult.Length, Is.GreaterThanOrEqualTo(info.Length));
		Assert.That(orResult.Length, Is.GreaterThanOrEqualTo(debug.Length));
		Assert.That(info.Length + notInfo.Length, Is.EqualTo(all.Length));
	}

	[Test]
	public void Filter_ClearFilterCacheDoesNotChangeResults()
	{
		using var reader = new LoggerReader(LogFilePath);
		reader.Process();

		const string query = "info & maui";
		var first = reader.Filter(query);
		reader.ClearFilterCache();
		var second = reader.Filter(query);

		AssertSameLines(first, second);
	}
}
