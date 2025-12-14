namespace FastScriptPhpLike.Core;

using System;
using System.Collections.Generic;
using System.Text;

public class Lexer
{
    private readonly string _source;
    private int _start = 0;
    private int _current = 0;
    private int _line = 1;
    private int _column = 1;
    private readonly List<Token> _tokens = new();

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        { "if", TokenType.If },
        { "elseif", TokenType.ElseIf },
        { "else if", TokenType.ElseIf }, // обрабатывается позже
        { "else", TokenType.Else },
        { "for", TokenType.For },
        { "foreach", TokenType.Foreach },
        { "while", TokenType.While },
        { "do", TokenType.Do },
        { "const", TokenType.Const },
        { "return", TokenType.Return },
        { "switch", TokenType.Switch },
        { "case", TokenType.Case },
        { "match", TokenType.Match },
        { "default", TokenType.Default },
        { "true", TokenType.True },
        { "false", TokenType.False },
        { "null", TokenType.Null }
    };

    public Lexer(string source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public List<Token> ScanTokens()
    {
        _tokens.Clear();
        while (!IsAtEnd())
        {
            _start = _current;
            ScanToken();
        }
        _tokens.Add(new Token(TokenType.Eof, null, _line, _column));
        return _tokens;
    }

    private void ScanToken()
    {
        char c = Advance();
        switch (c)
        {
            case ' ': case '\r': case '\t': break;
            case '\n': _line++; _column = 1; break;

            case '(': AddToken(TokenType.LParen); break;
            case ')': AddToken(TokenType.RParen); break;
            case '{': AddToken(TokenType.LBrace); break;
            case '}': AddToken(TokenType.RBrace); break;
            case '[': AddToken(TokenType.LBracket); break;
            case ']': AddToken(TokenType.RBracket); break;
            case ';': AddToken(TokenType.Semicolon); break;
            case ',': AddToken(TokenType.Comma); break;
            case ':': AddToken(TokenType.Colon); break;

            case '.': AddToken(TokenType.Concat); break;

            case '+':
                if (Match('+')) AddToken(TokenType.PlusPlus);
                else if (Match('=')) AddToken(TokenType.PlusEq);
                else AddToken(TokenType.Plus);
                break;

            case '-':
                if (Match('>')) AddToken(TokenType.Arrow);
                else if (Match('-')) AddToken(TokenType.MinusMinus);
                else if (Match('=')) AddToken(TokenType.MinusEq);
                else AddToken(TokenType.Minus);
                break;

            case '*':
                if (Match('=')) AddToken(TokenType.StarEq);
                else AddToken(TokenType.Star);
                break;

            case '/':
                if (Match('/')) { // однострочный комментарий
                    while (Peek() != '\n' && !IsAtEnd()) Advance();
                }
                else if (Match('*')) { // многострочный комментарий
                    while (!(Peek() == '*' && PeekNext() == '/') && !IsAtEnd())
                    {
                        if (Advance() == '\n') { _line++; _column = 1; }
                    }
                    if (!IsAtEnd()) { Advance(); Advance(); } // skip */
                }
                else if (Match('=')) AddToken(TokenType.SlashEq);
                else AddToken(TokenType.Slash);
                break;

            case '%':
                if (Match('=')) AddToken(TokenType.PercentEq);
                else AddToken(TokenType.Percent);
                break;

            case '=':
                if (Match('=')) AddToken(TokenType.EqEq);
                else AddToken(TokenType.Eq);
                break;

            case '!':
                if (Match('=')) AddToken(TokenType.NotEq);
                else AddToken(TokenType.Not);
                break;

            case '<':
                if (Match('<'))
                {
                    if (Match('=')) AddToken(TokenType.LeftShiftEq);
                    else AddToken(TokenType.LeftShift);
                }
                else if (Match('=')) AddToken(TokenType.LtEq);
                else AddToken(TokenType.Lt);
                break;

            case '>':
                if (Match('>'))
                {
                    if (Match('=')) AddToken(TokenType.RightShiftEq);
                    else AddToken(TokenType.RightShift);
                }
                else if (Match('=')) AddToken(TokenType.GtEq);
                else AddToken(TokenType.Gt);
                break;

            case '&':
                if (Match('&')) AddToken(TokenType.AndAnd);
                else if (Match('=')) AddToken(TokenType.BitAnd); // or custom
                else AddToken(TokenType.Amp);
                break;

            case '|':
                if (Match('|')) AddToken(TokenType.OrOr);
                else if (Match('=')) AddToken(TokenType.BitOr);
                else AddToken(TokenType.Pipe);
                break;

            case '^':
                if (Match('=')) AddToken(TokenType.BitXor);
                else AddToken(TokenType.BitXor);
                break;

            case '~':
                AddToken(TokenType.BitNot);
                break;

            case '"': ReadString(); break;
            case '\'': ReadString(); break;

            default:
                if (IsDigit(c)) ReadNumber();
                else if (IsAlpha(c)) ReadIdentifier();
                else
                {
                    // Ошибка: неизвестный символ
                    // Можно бросить исключение или добавить ErrorToken
                    throw new InvalidOperationException($"Unexpected character: '{c}' at line {_line}, col {_column - 1}");
                }
                break;
        }
    }

    private void ReadString()
    {
        char quote = _source[_start];
        while (Peek() != quote && !IsAtEnd())
        {
            if (Peek() == '\n') _line++;
            Advance();
        }

        if (IsAtEnd())
            throw new InvalidOperationException($"Unterminated string at line {_line}");

        Advance(); // закрывающая кавычка

        string value = _source.Substring(_start + 1, _current - _start - 2);
        AddToken(TokenType.StringLiteral, value);
    }

    private void ReadNumber()
    {
        while (IsDigit(Peek())) Advance();

        if (Peek() == '.' && IsDigit(PeekNext()))
        {
            Advance(); // '.'
            while (IsDigit(Peek())) Advance();
            AddToken(TokenType.FloatLiteral, _source.Substring(_start, _current - _start));
        }
        else
        {
            AddToken(TokenType.IntLiteral, _source.Substring(_start, _current - _start));
        }
    }

    private void ReadIdentifier()
    {
        while (IsAlphaNumeric(Peek())) Advance();

        string text = _source.Substring(_start, _current - _start);

        // Особый кейс: "else if" как один токен ElseIf
        if (text == "else" && Peek() == ' ' && StartsWith("if", _current + 1))
        {
            // пропускаем пробел и "if"
            int i = _current + 1;
            while (i < _source.Length && char.IsWhiteSpace(_source[i])) i++;
            if (_source.Substring(i, Math.Min(2, _source.Length - i)) == "if")
            {
                // перескакиваем на конец "else if"
                _current = i + 2;
                AddToken(TokenType.ElseIf, "else if");
                return;
            }
        }

        if (Keywords.TryGetValue(text, out TokenType type))
            AddToken(type, text);
        else
            AddToken(TokenType.Identifier, text);
    }

    private bool StartsWith(string needle, int offset)
    {
        if (offset + needle.Length > _source.Length) return false;
        for (int i = 0; i < needle.Length; i++)
            if (_source[offset + i] != needle[i]) return false;
        return true;
    }

    private char Advance()
    {
        _column++;
        return _source[_current++];
    }

    private char Peek()
    {
        if (IsAtEnd()) return '\0';
        return _source[_current];
    }

    private char PeekNext()
    {
        if (_current + 1 >= _source.Length) return '\0';
        return _source[_current + 1];
    }

    private bool Match(char expected)
    {
        if (IsAtEnd()) return false;
        if (_source[_current] != expected) return false;
        _current++;
        _column++;
        return true;
    }

    private bool IsAtEnd() => _current >= _source.Length;

    private static bool IsDigit(char c) => c >= '0' && c <= '9';

    private static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';

    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || IsDigit(c);

    private void AddToken(TokenType type, string? lexeme = null)
    {
        _tokens.Add(new Token(type, lexeme ?? _source.Substring(_start, _current - _start), _line, _column - (_current - _start)));
    }
}