import { useRef } from 'react'
import { Input } from 'antd'
import { useLiveTransliterate } from '../hooks/useLiveTransliterate'

/**
 * A controlled single-line Input that live-transliterates each word into the
 * selected language's script the instant it's finished (on a word boundary).
 * See hooks/useLiveTransliterate.js for the conversion mechanics.
 */
export default function TransliterateInput({ value, onChange, language, ...rest }) {
  const inputRef = useRef(null)
  const { handleChange, trackCursor } = useLiveTransliterate({
    value, onChange, language,
    getNativeEl: () => inputRef.current?.input,
  })

  return (
    <Input
      ref={inputRef}
      value={value}
      onChange={handleChange}
      onKeyUp={trackCursor}
      onClick={trackCursor}
      {...rest}
    />
  )
}
