import { Extension } from '@tiptap/core'
import Suggestion, { type SuggestionOptions, type SuggestionProps, type SuggestionKeyDownProps } from '@tiptap/suggestion'
import { PluginKey } from '@tiptap/pm/state'

export interface NoteItem {
  path: string
  name: string
}

const pluginKey = new PluginKey('wikiLinkSuggestion')

/**
 * Extracts the display name from a note path, stripping folders and .md extension.
 * Mirrors the logic in WikiLinkAutocomplete.vue.
 */
function pathToName(path: string): string {
  const filename = path.split('/').pop() ?? path
  return filename.endsWith('.md') ? filename.slice(0, -3) : filename
}

/**
 * Creates a Tiptap extension that provides wiki-link autocomplete when typing `[[`.
 * Uses @tiptap/suggestion under the hood with a custom trigger matcher for `[[`.
 */
export function createWikiLinkSuggestion(options: {
  allPaths: () => string[]
}) {
  return Extension.create({
    name: 'wikiLinkSuggestion',

    addProseMirrorPlugins() {
      return [
        Suggestion<NoteItem, NoteItem>({
          editor: this.editor,
          pluginKey,
          char: '[',
          allowSpaces: true,
          allowedPrefixes: null,

          // Custom match: require `[[` (not just `[`)
          findSuggestionMatch(config) {
            const { $position } = config

            // Use doc.textBetween with a placeholder for leaf nodes (hardBreak etc.)
            // so each node = 1 char = 1 document position, keeping indices aligned.
            const nodeStart = $position.pos - $position.parentOffset
            const textBefore = $position.doc.textBetween(nodeStart, $position.pos, '', '\0')

            // Find last `[[` that isn't closed
            const triggerIdx = textBefore.lastIndexOf('[[')
            if (triggerIdx === -1) return null

            const between = textBefore.substring(triggerIdx + 2)
            if (between.includes(']]') || between.includes('\n') || between.includes('\0')) return null

            const from = nodeStart + triggerIdx
            const to = $position.pos

            return {
              range: { from, to },
              query: between,
              text: textBefore.substring(triggerIdx),
            }
          },

          items({ query }) {
            const paths = options.allPaths()
            const q = query.toLowerCase()

            if (!q) {
              return paths.slice(0, 15).map(p => ({ path: p, name: pathToName(p) }))
            }

            return paths
              .map(p => ({ path: p, name: pathToName(p) }))
              .filter(item => item.name.toLowerCase().includes(q) || item.path.toLowerCase().includes(q))
              .sort((a, b) => {
                const aStarts = a.name.toLowerCase().startsWith(q) ? 0 : 1
                const bStarts = b.name.toLowerCase().startsWith(q) ? 0 : 1
                if (aStarts !== bStarts) return aStarts - bStarts
                return a.name.localeCompare(b.name)
              })
              .slice(0, 15)
          },

          command({ editor, range, props: item }) {
            const allPaths = options.allPaths()
            const isAmbiguous = allPaths.filter(p => pathToName(p) === item.name).length > 1
            const raw = isAmbiguous ? `${item.path}|${item.name}` : item.name
            const resolvedPath = item.path.endsWith('.md') ? item.path : `${item.path}.md`

            // Replace the trigger text with a proper wiki-link mark in a single transaction
            const { schema } = editor.state
            const markType = schema.marks.wikiLink
            if (!markType) return

            const mark = markType.create({ path: resolvedPath, raw })
            const linkNode = schema.text(item.name, [mark])
            const spaceNode = schema.text(' ')

            editor.view.dispatch(
              editor.state.tr
                .replaceWith(range.from, range.to, [linkNode, spaceNode])
            )
          },

          render: () => createSuggestionRenderer(),
        } satisfies Partial<SuggestionOptions<NoteItem, NoteItem>> as any),
      ]
    },
  })
}

/**
 * Creates the DOM-based suggestion dropdown renderer.
 * Handles positioning, keyboard navigation, and selection.
 */
function createSuggestionRenderer() {
  let dropdown: HTMLDivElement | null = null
  let selectedIndex = 0
  let currentItems: NoteItem[] = []
  let currentCommand: ((item: NoteItem) => void) | null = null

  function updateDropdown() {
    if (!dropdown) return
    dropdown.innerHTML = ''

    currentItems.forEach((item, i) => {
      const row = document.createElement('div')
      row.className = 'wl-suggest-item'
      if (i === selectedIndex) row.classList.add('wl-suggest-item--active')

      // Icon
      const icon = document.createElement('svg')
      icon.setAttribute('viewBox', '0 0 20 20')
      icon.setAttribute('fill', 'currentColor')
      icon.className = 'wl-suggest-icon'
      icon.innerHTML = '<path fill-rule="evenodd" d="M4 4a2 2 0 012-2h4.586A2 2 0 0112 2.586L15.414 6A2 2 0 0116 7.414V16a2 2 0 01-2 2H6a2 2 0 01-2-2V4z" clip-rule="evenodd" />'

      const textWrap = document.createElement('div')
      textWrap.className = 'wl-suggest-text'

      const nameEl = document.createElement('div')
      nameEl.className = 'wl-suggest-name'
      nameEl.textContent = item.name

      textWrap.appendChild(nameEl)

      // Show full path if different from just name.md
      if (item.path !== `${item.name}.md`) {
        const pathEl = document.createElement('div')
        pathEl.className = 'wl-suggest-path'
        if (i === selectedIndex) pathEl.classList.add('wl-suggest-path--active')
        pathEl.textContent = item.path
        textWrap.appendChild(pathEl)
      }

      row.appendChild(icon)
      row.appendChild(textWrap)

      row.addEventListener('mousedown', (e) => {
        e.preventDefault()
        currentCommand?.(item)
      })
      row.addEventListener('mouseenter', () => {
        selectedIndex = i
        updateDropdown()
      })

      dropdown!.appendChild(row)
    })
  }

  function positionDropdown(clientRect: (() => DOMRect | null) | null | undefined) {
    if (!dropdown || !clientRect) return
    const rect = clientRect()
    if (!rect) return

    const dropdownHeight = dropdown.offsetHeight || 240
    const viewportHeight = window.innerHeight
    const spaceBelow = viewportHeight - rect.bottom
    const spaceAbove = rect.top

    // Flip above if not enough space below
    if (spaceBelow < dropdownHeight + 8 && spaceAbove > spaceBelow) {
      dropdown.style.top = ''
      dropdown.style.bottom = `${viewportHeight - rect.top + 4}px`
    } else {
      dropdown.style.bottom = ''
      dropdown.style.top = `${rect.bottom + 4}px`
    }
    dropdown.style.left = `${rect.left}px`
  }

  return {
    onStart(props: SuggestionProps<NoteItem, NoteItem>) {
      dropdown = document.createElement('div')
      dropdown.className = 'wl-suggest-dropdown'
      document.body.appendChild(dropdown)

      selectedIndex = 0
      currentItems = props.items
      currentCommand = props.command
      updateDropdown()
      positionDropdown(props.clientRect)
    },

    onUpdate(props: SuggestionProps<NoteItem, NoteItem>) {
      currentItems = props.items
      currentCommand = props.command
      selectedIndex = 0
      updateDropdown()
      positionDropdown(props.clientRect)
    },

    onKeyDown(props: SuggestionKeyDownProps) {
      const { event } = props

      if (event.key === 'ArrowDown') {
        event.preventDefault()
        selectedIndex = Math.min(selectedIndex + 1, currentItems.length - 1)
        updateDropdown()
        return true
      }

      if (event.key === 'ArrowUp') {
        event.preventDefault()
        selectedIndex = Math.max(selectedIndex - 1, 0)
        updateDropdown()
        return true
      }

      if (event.key === 'Enter' || event.key === 'Tab') {
        if (currentItems.length > 0) {
          event.preventDefault()
          currentCommand?.(currentItems[selectedIndex]!)
          return true
        }
      }

      if (event.key === 'Escape') {
        event.preventDefault()
        dropdown?.remove()
        dropdown = null
        return true
      }

      return false
    },

    onExit() {
      dropdown?.remove()
      dropdown = null
      currentItems = []
      currentCommand = null
    },
  }
}
