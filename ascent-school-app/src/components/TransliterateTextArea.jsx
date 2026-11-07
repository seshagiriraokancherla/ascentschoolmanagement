import { useRef } from 'react'
import { Input } from 'antd'
import { useLiveTransliterate } from '../hooks/useLiveTransliterate'

const { TextArea } = Input

/**
 * A controlled Input.TextArea that live-transliterates each word into the
 * selected language's script the instant it's finished (on a word boundary).
 * See hooks/useLiveTransliterate.js for the conversion mechanics.
 */
export default function TransliterateTextArea({ value, onChange, language, ...rest }) {
  const taRef = useRef(null)
  const { handleChange, trackCursor } = useLiveTransliterate({
    value, onChange, language,
    getNativeEl: () => taRef.current?.resizableTextArea?.textArea,
  })

  return (
    <TextArea
      ref={taRef}
      value={value}
      onChange={handleChange}
      onKeyUp={trackCursor}
      onClick={trackCursor}
      {...rest}
    />
  )
}
