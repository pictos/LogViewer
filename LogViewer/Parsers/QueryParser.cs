using System.Runtime.CompilerServices;
using System.Text;

namespace LogViewer.Parsers;

// ── AST nodes ────────────────────────────────────────────────────────────────

public abstract class QueryNode
{
	public abstract bool Matches(ReadOnlySpan<byte> line);

	/// <summary>
	/// A stable, case-insensitive key identifying the predicate this node represents. Two nodes
	/// with the same key always match the same lines, so it is safe to use as a cache key.
	/// </summary>
	public abstract string Key { get; }
}

/// <summary>Matches lines that contain the literal <paramref name="term"/> (case-insensitive ASCII).</summary>
public sealed class TermNode : QueryNode
{
	readonly byte[] lowerUtf8;

	public TermNode(string term)
	{
		var bytes = Encoding.UTF8.GetBytes(term);
		for (var i = 0; i < bytes.Length; i++)
			bytes[i] = LoggerReader.ToLowerAscii(bytes[i]);
		lowerUtf8 = bytes;

		// Case-insensitive matching means "Info" and "info" are the same predicate.
		Key = term.ToLowerInvariant();
	}

	public override string Key { get; }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) =>
		LoggerReader.IndexOfIgnoreCaseAscii(line, lowerUtf8) >= 0;
}

/// <summary>Matches lines where both operands match (short-circuits on first failure).</summary>
public sealed class AndNode(QueryNode left, QueryNode right) : QueryNode
{
	public QueryNode Left => left;
	public QueryNode Right => right;

	public override string Key => $"({left.Key}&{right.Key})";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) => left.Matches(line) && right.Matches(line);
}

/// <summary>Matches lines where at least one operand matches (short-circuits on first success).</summary>
public sealed class OrNode(QueryNode left, QueryNode right) : QueryNode
{
	public QueryNode Left => left;
	public QueryNode Right => right;

	public override string Key => $"({left.Key}|{right.Key})";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) => left.Matches(line) || right.Matches(line);
}

/// <summary>Matches lines where the inner operand does NOT match.</summary>
public sealed class NotNode(QueryNode operand) : QueryNode
{
	public QueryNode Operand => operand;

	public override string Key => $"!{operand.Key}";

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) => !operand.Matches(line);
}

// ── Parser ────────────────────────────────────────────────────────────────────

/// <summary>
/// Parses a boolean query string into a <see cref="QueryNode"/> tree.
/// <para>
/// Syntax (in order of decreasing precedence):
/// <list type="bullet">
///   <item><description><c>!expr</c> — NOT</description></item>
///   <item><description><c>A &amp; B</c> — AND</description></item>
///   <item><description><c>A | B</c> — OR</description></item>
///   <item><description><c>(expr)</c> — grouping</description></item>
///   <item><description><c>word</c> or <c>"quoted phrase"</c> — literal term</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class QueryParser
{
	readonly string input;
	int pos;

	QueryParser(string input) => this.input = input;

	/// <summary>Parses <paramref name="query"/> and returns the root <see cref="QueryNode"/>.</summary>
	/// <exception cref="FormatException">Thrown when the query is syntactically invalid.</exception>
	public static QueryNode Parse(string query)
	{
		ArgumentException.ThrowIfNullOrEmpty(query);
		var parser = new QueryParser(query.Trim());
		var node = parser.ParseOr();
		parser.SkipWhitespace();
		if (parser.pos < parser.input.Length)
			throw new FormatException(
				$"Unexpected character '{parser.input[parser.pos]}' at position {parser.pos}.");
		return node;
	}

	void SkipWhitespace()
	{
		while (pos < input.Length && char.IsWhiteSpace(input[pos]))
			pos++;
	}

	// expr = or_expr
	// or_expr  = and_expr ('|' and_expr)*
	QueryNode ParseOr()
	{
		var left = ParseAnd();
		while (true)
		{
			SkipWhitespace();
			if (pos >= input.Length || input[pos] != '|') break;
			pos++;
			left = new OrNode(left, ParseAnd());
		}
		return left;
	}

	// and_expr = unary ('&' unary)*
	QueryNode ParseAnd()
	{
		var left = ParseUnary();
		while (true)
		{
			SkipWhitespace();
			if (pos >= input.Length || input[pos] != '&') break;
			pos++;
			left = new AndNode(left, ParseUnary());
		}
		return left;
	}

	// unary = '!' unary | primary
	QueryNode ParseUnary()
	{
		SkipWhitespace();
		if (pos < input.Length && input[pos] == '!')
		{
			pos++;
			return new NotNode(ParseUnary());
		}
		return ParsePrimary();
	}

	// primary = '(' expr ')' | term
	QueryNode ParsePrimary()
	{
		SkipWhitespace();
		if (pos < input.Length && input[pos] == '(')
		{
			pos++;
			var inner = ParseOr();
			SkipWhitespace();
			if (pos >= input.Length || input[pos] != ')')
				throw new FormatException("Missing closing ')'.");
			pos++;
			return inner;
		}
		return ParseTerm();
	}

	// term = '"' [^"]* '"' | <chars that are not operators/whitespace/parens>
	QueryNode ParseTerm()
	{
		SkipWhitespace();
		if (pos >= input.Length)
			throw new FormatException("Expected a search term but reached end of query.");

		if (input[pos] == '"')
		{
			pos++;
			var start = pos;
			while (pos < input.Length && input[pos] != '"')
				pos++;
			if (pos >= input.Length)
				throw new FormatException("Unclosed quoted string.");
			var value = input[start..pos];
			pos++; // consume closing "
			if (value.Length == 0)
				throw new FormatException("Empty quoted term is not allowed.");
			return new TermNode(value);
		}

		var termStart = pos;
		while (pos < input.Length && !IsTermStop(input[pos]))
			pos++;

		var term = input[termStart..pos].Trim();
		if (term.Length == 0)
			throw new FormatException($"Expected a search term at position {termStart}.");

		return new TermNode(term);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IsTermStop(char c) => c is ' ' or '\t' or '&' or '|' or '!' or '(' or ')' or '"';
}
