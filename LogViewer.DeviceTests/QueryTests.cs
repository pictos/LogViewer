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
	static readonly ReadOnlyMemory<byte>[] BooleanTruthTableInputs =
	[
		""u8.ToArray(),
		"info"u8.ToArray(),
		"debug"u8.ToArray(),
		"error"u8.ToArray(),
		"info debug"u8.ToArray(),
		"info error"u8.ToArray(),
		"debug error"u8.ToArray(),
		"info debug error"u8.ToArray()
	];

	static void AssertSameLines(ImmutableArray<LogInfo> expected, ImmutableArray<LogInfo> actual)
	{
		Assert.That(actual.Length, Is.EqualTo(expected.Length));
		for (var i = 0; i < expected.Length; i++)
			Assert.That(actual[i].Text, Is.EqualTo(expected[i].Text), $"Mismatch at index {i}");
	}

	static bool[] EvaluateTruthTable(string query)
	{
		var node = QueryParser.Parse(query);
		var results = new bool[BooleanTruthTableInputs.Length];
		for (var i = 0; i < BooleanTruthTableInputs.Length; i++)
			results[i] = node.Matches(BooleanTruthTableInputs[i].Span);
		return results;
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

	[TestCase("!(info & debug)", "!info | !debug")]
	[TestCase("!(info | debug)", "!info & !debug")]
	[TestCase("info & (debug | error)", "(info & debug) | (info & error)")]
	[TestCase("!!info", "info")]
	[TestCase("info & debug", "debug & info")]
	[TestCase("info | debug", "debug | info")]
	public void Parse_BooleanConstructionsThatShouldBeEquivalent_AreEquivalent(string left, string right)
	{
		Assert.That(EvaluateTruthTable(left), Is.EqualTo(EvaluateTruthTable(right)));
	}

	[Test]
	public void Parse_BooleanConstructionsThatShouldDiffer_AreNotEquivalent()
	{
		Assert.That(
			EvaluateTruthTable("!info & !debug"),
			Is.Not.EqualTo(EvaluateTruthTable("!(info & debug)")));
	}

	[TestCase("!(info & debug)", "!info | !debug")]
	[TestCase("!(info | debug)", "!info & !debug")]
	[TestCase("!!info", "info")]
	public void Filter_EquivalentBooleanConstructionsReturnSameResults(string left, string right)
	{
		using var reader = new LoggerReader(LogFilePath);
		reader.Process();

		AssertSameLines(reader.Filter(left), reader.Filter(right));
	}
}
