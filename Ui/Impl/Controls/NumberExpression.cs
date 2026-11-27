using System.Globalization;

namespace EnsembleRoot.Ui.Controls;

/// <summary>Evaluates arithmetic typed into a number box: numbers, <c>+ - * /</c>, and parentheses.</summary>
public static class NumberExpression
{
	private const NumberStyles NumberStyle = NumberStyles.Number | NumberStyles.AllowExponent;

	public static bool TryEvaluate(string text, NumberFormatInfo format, out decimal value)
	{
		try
		{
			value = Evaluate(text, format);
			return true;
		}
		catch (Exception e) when (e is FormatException or OverflowException or DivideByZeroException)
		{
			value = 0;
			return false;
		}
	}

	/// <exception cref="FormatException">The text isn't an expression.</exception>
	/// <exception cref="OverflowException">A number or result is out of the decimal range.</exception>
	/// <exception cref="DivideByZeroException">The expression divides by zero.</exception>
	public static decimal Evaluate(string text, NumberFormatInfo format)
	{
		var parser = new Parser(text, format);
		var value = parser.ParseSum();

		return parser.IsAtEnd ? value : throw parser.Unexpected();
	}

	private sealed class Parser(string text, NumberFormatInfo format)
	{
		private int _position;

		public bool IsAtEnd => Peek() is null;

		public decimal ParseSum()
		{
			var value = ParseProduct();

			while (true)
				if (Accept('+'))
					value += ParseProduct();
				else if (Accept('-') || Accept(format.NegativeSign))
					value -= ParseProduct();
				else
					return value;
		}

		public FormatException Unexpected() =>
			new(_position < text.Length ? $"Unexpected '{text[_position]}' in '{text}'." : $"'{text}' ends early.");

		private decimal ParseProduct()
		{
			var value = ParseUnary();

			while (true)
				if (Accept('*'))
					value *= ParseUnary();
				else if (Accept('/'))
					value /= ParseUnary();
				else
					return value;
		}

		private decimal ParseUnary()
		{
			var sign = 1;

			while (true)
				if (Accept('-') || Accept(format.NegativeSign))
					sign = -sign;
				else if (!Accept('+'))
					return sign * ParsePrimary();
		}

		private decimal ParsePrimary()
		{
			if (Accept('('))
			{
				var value = ParseSum();
				return Accept(')') ? value : throw Unexpected();
			}

			var start = _position;

			while (_position < text.Length && GetNumberPartLength() is > 0 and var length)
				_position += length;

			return _position > start
				? decimal.Parse(text.AsSpan(start, _position - start), NumberStyle, format)
				: throw Unexpected();
		}

		// An exponent's sign belongs to the number, not to a sum
		private int GetNumberPartLength()
		{
			var c = text[_position];

			if (char.IsAsciiDigit(c) || c is 'e' or 'E' || (c is '+' or '-' && text[_position - 1] is 'e' or 'E'))
				return 1;

			if (IsAt(format.NumberDecimalSeparator))
				return format.NumberDecimalSeparator.Length;

			return IsAt(format.NumberGroupSeparator) ? format.NumberGroupSeparator.Length : 0;
		}

		private bool IsAt(string token) =>
			token.Length > 0 && text.AsSpan(_position).StartsWith(token, StringComparison.Ordinal);

		private char? Peek()
		{
			while (_position < text.Length && char.IsWhiteSpace(text[_position]))
				_position++;

			return _position < text.Length ? text[_position] : null;
		}

		private bool Accept(char token)
		{
			if (Peek() != token)
				return false;

			_position++;
			return true;
		}

		private bool Accept(string token)
		{
			Peek();

			if (!IsAt(token))
				return false;

			_position += token.Length;
			return true;
		}
	}
}
