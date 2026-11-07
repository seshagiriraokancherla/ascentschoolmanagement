import { useEffect, useRef } from 'react'
import { transliterateWord, WORD_BOUNDARY_RE } from '../utils/transliteration'

/**
 * Shared live word-by-word transliteration logic, used by both the single-line
 * TransliterateInput and multi-line TransliterateTextArea wrappers. See
 * utils/transliteration.js for the conversion rule and its known limitation.
 *
 * `getNativeEl` must return the actual DOM <input>/<textarea> element — AntD's
 * Input and Input.TextArea expose it differently via their own ref object
 * (`.input` vs `.resizableTextArea.textArea`), so each caller supplies its own.
 */
export function useLiveTransliterate({ value, onChange, language, getNativeEl }) {
  const valueRef  = useRef(value || '')
  const cursorRef = useRef(0)

  // Keep the tracked value in sync when the parent changes it externally
  // (e.g. opening the modal for a different record) — harmless when it's
  // already in sync with our own just-applied update. Runs after render,
  // never during it.
  useEffect(() => {
    const el = getNativeEl()
    if (valueRef.current !== value && document.activeElement !== el) {
      valueRef.current = value || ''
    }
  }, [value])

  const handleChange = (e) => {
    const newValue  = e.target.value
    const newCursor = e.target.selectionStart
    const oldValue  = valueRef.current
    const oldCursor = cursorRef.current

    // Only attempt live conversion for a simple single-character insertion at
    // the cursor (the normal typing case) — paste/delete/multi-char edits pass
    // through untouched rather than risking a wrong guess.
    const isSimpleInsert =
      language && language !== 'english' &&
      newValue.length === oldValue.length + 1 &&
      newCursor === oldCursor + 1 &&
      newValue.slice(0, oldCursor) === oldValue.slice(0, oldCursor) &&
      newValue.slice(newCursor) === oldValue.slice(oldCursor)

    if (isSimpleInsert && WORD_BOUNDARY_RE.test(newValue[newCursor - 1])) {
      let start = newCursor - 1
      while (start > 0 && !WORD_BOUNDARY_RE.test(newValue[start - 1])) start--
      const word      = newValue.slice(start, newCursor - 1)
      const converted = transliterateWord(word, language)

      if (converted !== word) {
        const spliced = newValue.slice(0, start) + converted + newValue.slice(newCursor - 1)
        const newPos  = start + converted.length + 1

        valueRef.current  = spliced
        cursorRef.current = newPos
        onChange(spliced)

        requestAnimationFrame(() => {
          const el = getNativeEl()
          if (el) el.setSelectionRange(newPos, newPos)
        })
        return
      }
    }

    valueRef.current  = newValue
    cursorRef.current = newCursor
    onChange(newValue)
  }

  const trackCursor = (e) => { cursorRef.current = e.target.selectionStart }

  return { handleChange, trackCursor }
}
