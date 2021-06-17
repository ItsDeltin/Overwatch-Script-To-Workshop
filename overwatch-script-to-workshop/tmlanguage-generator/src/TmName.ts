export type TmName =
    // Specified in official naming conventions
    'comment' |
    'comment.line' |
    'comment.line.double-slash' |
    'comment.line.double-dash' |
    'comment.line.number-sign' |
    'comment.line.percentage' |
    'comment.block' |
    'comment.block.documentation' |
    'constant' |
    'constant.numeric' |
    'constant.character' |
    'constant.character.escape' |
    'constant.language' |
    'constant.other' |
    'entity' |
    'entity.name' |
    'entity.name.function' |
    'entity.name.type' |
    'entity.name.tag' |
    'entity.name.section' |
    'entity.other' |
    'entity.other.inherited-class' |
    'entity.other.attribute-name' |
    'invalid' |
    'invalid.illegal' |
    'invalid.deprecated' |
    'keyword' |
    'keyword.control' |
    'keyword.operator' |
    'keyword.other' |
    'markup' |
    'markup.underline' |
    'markup.underline.link' |
    'markup.bold' |
    'markup.heading' |
    'markup.italic' |
    'markup.list' |
    'markup.list.numbered' |
    'markup.list.unnumbered' |
    'markup.quote' |
    'markup.raw' |
    'markup.other' |
    'meta' |
    'meta.function' |
    'storage' |
    'storage.type' |
    'storage.modifier' |
    'string' |
    'string.quoted' |
    'string.quoted.single' |
    'string.quoted.double' |
    'string.quoted.triple' |
    'string.quoted.other' |
    'string.unquoted' |
    'string.interpolated' |
    'string.regexp' |
    'string.other' |
    'support' |
    'support.function' |
    'support.class' |
    'support.type' |
    'support.constant' |
    'support.variable' |
    'support.other' |
    'variable' |
    'variable.parameter' |
    'variable.language' |
    'variable.other' |

    // Not in conventions but commonly used.
    'constant.language.boolean.false' | // false
    'constant.language.boolean.true' | // true
    'constant.language.null' | // null
    'keyword.control.conditional.else' | // else
    'keyword.control.conditional.if' | // if
    'keyword.control.flow' | // return etc
    'keyword.control.loop.for' | // for
    'keyword.control.loop.foreach' | // foreach
    'keyword.control.loop.in' | // in
    'keyword.control.loop.while' | // while
    'keyword.control.switch' | // switch
    'keyword.operator.arithmetic' | // % * / - + ^
    'keyword.operator.assignment.compound' | // = += -= *= /= %= ^= ++ --
    'keyword.operator.assignment' |
    'keyword.operator.comparison' | // == !=
    'keyword.operator.conditional.colon' | // :
    'keyword.operator.conditional.question-mark' | // ?:
    'keyword.operator.logical' | // && || !
    'keyword.operator.relational' | // <= >= < >
    'keyword.operator.type' |
    'keyword.operator.type' | // |
    'keyword.other.new' | // new
    'punctuation.accessor' | // x.y
    'punctuation.definition.block' | // { }
    'punctuation.definition.interpolation.begin' | // $ {
    'punctuation.definition.interpolation.end' | // $ }
    'punctuation.definition.typeparameters.begin' | // <
    'punctuation.definition.typeparameters.end' | // >
    'punctuation.parenthesis.close' | // )
    'punctuation.parenthesis.open' | // (
    'punctuation.separator.comma' | // ,
    'punctuation.squarebracket.close' | // ]
    'punctuation.squarebracket.open' | // [
    'punctuation.terminator.statement' | // ;
    'storage.type.function.arrow' | // => -=

    // To make the name unrestricted.
    (string & { customName?: any });