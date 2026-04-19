/**
 * Returns true if `newContent` equals `oldContent` plus a non-empty suffix.
 * Used by WysiwygEditor to detect when an external content update is a
 * pure append (e.g., a tool writing a roll result to the end of the note)
 * so the editor can insert just the new suffix at the document end
 * instead of rebuilding the entire document from scratch.
 */
export function isPureAppend(oldContent: string, newContent: string): boolean {
  if (newContent === oldContent) return false
  if (newContent.length <= oldContent.length) return false
  return newContent.startsWith(oldContent)
}

/**
 * Returns true if the scroll container is within `threshold` pixels of its
 * bottom edge. Used by NoteEditor for sticky-scroll behavior: auto-scroll
 * to the new bottom when content is appended only if the user was already
 * near the bottom before the append, so we don't yank the view when the
 * user is scrolled up reading earlier content.
 */
export function isNearBottom(
  scrollTop: number,
  scrollHeight: number,
  clientHeight: number,
  threshold: number,
): boolean {
  if (!Number.isFinite(scrollTop) || !Number.isFinite(scrollHeight) || !Number.isFinite(clientHeight)) {
    return false
  }
  return scrollHeight - scrollTop - clientHeight < threshold
}
