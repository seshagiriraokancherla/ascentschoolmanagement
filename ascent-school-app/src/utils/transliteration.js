import Sanscript from '@indic-transliteration/sanscript'

// Live phonetic typing for homework Description fields. A word typed in plain
// lowercase Latin letters is transliterated into the selected script the moment
// it's finished (on a word boundary); anything else — ALL CAPS, digits, mixed
// case, punctuation — is left exactly as typed. This lets one line mix English
// instructions/headers (typed in caps, e.g. "PRACTICE", "PG: 52") with vernacular
// content (typed lowercase) with no separate toggle needed.
//
// Known limitation (accepted): the scheme below distinguishes dental vs retroflex
// consonants (e.g. Telugu త vs ట) by letter case in strict ITRANS, which we can't
// use here since any capital letter means "don't convert". Lowercase-only input
// always resolves to the dental form. Revisit with an escape hatch if needed.

export const LANGUAGE_OPTIONS = [
  { label: 'English', value: 'english' },
  { label: 'Telugu',  value: 'telugu' },
  { label: 'Hindi',   value: 'hindi' },
]

// itrans_dravidian (not plain itrans) is needed for Telugu — it adds the short
// e/o vowels Dravidian languages use that Sanskrit-only ITRANS doesn't have
// (e.g. "chettu" -> చెట్టు, not the long-vowel చేత్తు).
const SCHEMES = {
  telugu: { from: 'itrans_dravidian', to: 'telugu',     options: {} },
  hindi:  { from: 'itrans',           to: 'devanagari', options: { syncope: true } },
}

const isPureLowercaseWord = (word) => /^[a-z]+$/.test(word)

export function transliterateWord(word, language) {
  if (!word || language === 'english' || !language) return word
  const scheme = SCHEMES[language]
  if (!scheme || !isPureLowercaseWord(word)) return word
  try {
    return Sanscript.t(word, scheme.from, scheme.to, scheme.options)
  } catch {
    return word
  }
}

// Boundary characters that end a "word" for live conversion purposes.
export const WORD_BOUNDARY_RE = /[\s,.;:()&/\\"'!?]/
