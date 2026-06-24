using System.Runtime.CompilerServices;
using System.Text;

namespace LogViewer.Parsers;

// ── AST nodes ────────────────────────────────────────────────────────────────

public abstract class QueryNode
{
	public abstract bool Matches(ReadOnlySpan<byte> line);
}

/// <summary>Matches lines that contain the literal <paramref name="term"/> (case-insensitive ASCII).</summary>
public sealed class TermNode : QueryNode
{
	readonly byte[] _lowerUtf8;

	public TermNode(string term)
	{
		var bytes = Encoding.UTF8.GetBytes(term);
		for (var i = 0; i < bytes.Length; i++)
			bytes[i] = LoggerReader.ToLowerAscii(bytes[i]);
		_lowerUtf8 = bytes;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) =>
		LoggerReader.IndexOfIgnoreCaseAscii(line, _lowerUtf8) >= 0;
}

/// <summary>Matches lines where both operands match (short-circuits on first failure).</summary>
public sealed class AndNode(QueryNode left, QueryNode right) : QueryNode
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) => left.Matches(line) && right.Matches(line);
}

/// <summary>Matches lines where at least one operand matches (short-circuits on first success).</summary>
public sealed class OrNode(QueryNode left, QueryNode right) : QueryNode
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Matches(ReadOnlySpan<byte> line) => left.Matches(line) || right.Matches(line);
}

/// <summary>Matches lines where the inner operand does NOT match.</summary>
public sealed class NotNode(QueryNode operand) : QueryNode
{
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
	readonly string _input;
	int _pos;

	QueryParser(string input) => _input = input;

	/// <summary>Parses <paramref name="query"/> and returns the root <see cref="QueryNode"/>.</summary>
	/// <exception cref="FormatException">Thrown when the query is syntactically invalid.</exception>
	public static QueryNode Parse(string query)
	{
		ArgumentException.ThrowIfNullOrEmpty(query);
		var parser = new QueryParser(query.Trim());
		var node = parser.ParseOr();
		parser.SkipWhitespace();
		if (parser._pos < parser._input.Length)
			throw new FormatException(
				$"Unexpected character '{parser._input[parser._pos]}' at position {parser._pos}.");
		return node;
	}

	void SkipWhitespace()
	{
		while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos]))
			_pos++;
	}

	// expr = or_expr
	// or_expr  = and_expr ('|' and_expr)*
	QueryNode ParseOr()
	{
		var left = ParseAnd();
		while (true)
		{
			SkipWhitespace();
			if (_pos >= _input.Length || _input[_pos] != '|') break;
			_pos++;
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
			if (_pos >= _input.Length || _input[_pos] != '&') break;
			_pos++;
			left = new AndNode(left, ParseUnary());
		}
		return left;
	}

	// unary = '!' unary | primary
	QueryNode ParseUnary()
	{
		SkipWhitespace();
		if (_pos < _input.Length && _input[_pos] == '!')
		{
			_pos++;
			return new NotNode(ParseUnary());
		}
		return ParsePrimary();
	}

	// primary = '(' expr ')' | term
	QueryNode ParsePrimary()
	{
		SkipWhitespace();
		if (_pos < _input.Length && _input[_pos] == '(')
		{
			_pos++;
			var inner = ParseOr();
			SkipWhitespace();
			if (_pos >= _input.Length || _input[_pos] != ')')
				throw new FormatException("Missing closing ')'.");
			_pos++;
			return inner;
		}
		return ParseTerm();
	}

	// term = '"' [^"]* '"' | <chars that are not operators/whitespace/parens>
	QueryNode ParseTerm()
	{
		SkipWhitespace();
		if (_pos >= _input.Length)
			throw new FormatException("Expected a search term but reached end of query.");

		if (_input[_pos] == '"')
		{
			_pos++;
			var start = _pos;
			while (_pos < _input.Length && _input[_pos] != '"')
				_pos++;
			if (_pos >= _input.Length)
				throw new FormatException("Unclosed quoted string.");
			var value = _input[start.._pos];
			_pos++; // consume closing "
			if (value.Length == 0)
				throw new FormatException("Empty quoted term is not allowed.");
			return new TermNode(value);
		}

		var termStart = _pos;
		while (_pos < _input.Length && !IsTermStop(_input[_pos]))
			_pos++;

		var term = _input[termStart.._pos].Trim();
		if (term.Length == 0)
			throw new FormatException($"Expected a search term at position {termStart}.");

		return new TermNode(term);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	static bool IsTermStop(char c) => c is ' ' or '\t' or '&' or '|' or '!' or '(' or ')' or '"';
}
