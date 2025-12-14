namespace FastScriptPhpLike.Core;

public enum TokenType
{
    // Литералы
    IntLiteral,
    FloatLiteral,
    StringLiteral,
    True,
    False,
    Null,

    // Идентификаторы и ключевые слова
    Identifier,
    Const,
    Return,
    If,
    ElseIf,
    Else,
    For,
    Foreach,
    While,
    Do,
    Switch,
    Case,
    Match,
    Default,

    // Операторы
    Plus, Minus, Star, Slash, Percent,
    PlusPlus, MinusMinus,
    PlusEq, MinusEq, StarEq, SlashEq, PercentEq,
    Eq, EqEq, NotEq,
    Lt, Gt, LtEq, GtEq,
    And, Or, Not,
    AndAnd, OrOr,
    Pipe, PipePipe, // для match-выражений или future-расширений
    Amp, AmpAmp,
    Tilde,
    LeftShift, RightShift, LeftShiftEq, RightShiftEq,
    BitAnd, BitOr, BitXor, BitNot,
    Concat, // например: .
    ConcatEq,

    // Разделители
    LParen, RParen,
    LBrace, RBrace,
    LBracket, RBracket,
    Semicolon,
    Comma,
    Colon,
    Arrow, // => (если понадобится)

    // Конец файла
    Eof
}

public readonly struct Token
{
    public readonly TokenType Type;
    public readonly string? Lexeme; // null для EOF и некоторых операторов
    public readonly int Line, Column;

    public Token(TokenType type, string? lexeme, int line, int column)
    {
        Type = type;
        Lexeme = lexeme;
        Line = line;
        Column = column;
    }
}