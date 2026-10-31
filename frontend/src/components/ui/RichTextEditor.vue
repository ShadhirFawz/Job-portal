<script setup lang="ts">
import { watch, onBeforeUnmount } from 'vue'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import Underline from '@tiptap/extension-underline'
import {
  Bold as BoldIcon,
  Italic as ItalicIcon,
  Underline as UnderlineIcon,
  Strikethrough as StrikeIcon,
  Heading1,
  Heading2,
  Heading3,
  Quote,
  List,
  ListOrdered,
  Undo2,
  Redo2,
} from 'lucide-vue-next'

const props = withDefaults(
  defineProps<{
    modelValue: string
    placeholder?: string
    minHeight?: string
    disabled?: boolean
  }>(),
  {
    modelValue: '',
    placeholder: 'Enter details...',
    minHeight: '140px',
    disabled: false,
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

const editor = useEditor({
  content: props.modelValue,
  editable: !props.disabled,
  extensions: [
    StarterKit.configure({
      heading: {
        levels: [1, 2, 3],
      },
    }),
    Underline,
  ],
  editorProps: {
    attributes: {
      class: 'prose-sm focus:outline-none px-3.5 py-2.5 max-w-none text-gray-800 tiptap-content',
      style: `min-height: ${props.minHeight}`,
    },
  },
  onUpdate: () => {
    if (!editor.value) return
    const html = editor.value.isEmpty ? '' : editor.value.getHTML()
    emit('update:modelValue', html)
  },
})

// Sync external modelValue changes (e.g. form reset or pre-population)
watch(
  () => props.modelValue,
  (val) => {
    if (!editor.value) return
    const isSame = editor.value.getHTML() === val || (editor.value.isEmpty && !val)
    if (!isSame) {
      editor.value.commands.setContent(val || '', { emitUpdate: false })
    }
  }
)

watch(
  () => props.disabled,
  (val) => {
    editor.value?.setEditable(!val)
  }
)

onBeforeUnmount(() => {
  editor.value?.destroy()
})
</script>

<template>
  <div class="rich-text-wrapper border border-gray-300 rounded-lg overflow-hidden bg-white focus-within:ring-1 focus-within:ring-indigo-500 transition-colors shadow-xs">
    <!-- Toolbar -->
    <div
      v-if="editor && !disabled"
      class="flex flex-wrap items-center gap-0.5 px-2 py-1.5 bg-gray-50 border-b border-gray-200 text-gray-700 select-none"
    >
      <!-- Headings -->
      <button
        type="button"
        title="Heading 1"
        @click="editor.chain().focus().toggleHeading({ level: 1 }).run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('heading', { level: 1 }) ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <Heading1 class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Heading 2"
        @click="editor.chain().focus().toggleHeading({ level: 2 }).run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('heading', { level: 2 }) ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <Heading2 class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Heading 3"
        @click="editor.chain().focus().toggleHeading({ level: 3 }).run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('heading', { level: 3 }) ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <Heading3 class="h-4 w-4" />
      </button>

      <span class="w-px h-4 bg-gray-300 mx-1"></span>

      <!-- Inline formatting -->
      <button
        type="button"
        title="Bold (Ctrl+B)"
        @click="editor.chain().focus().toggleBold().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('bold') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <BoldIcon class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Italic (Ctrl+I)"
        @click="editor.chain().focus().toggleItalic().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('italic') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <ItalicIcon class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Underline (Ctrl+U)"
        @click="editor.chain().focus().toggleUnderline().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('underline') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <UnderlineIcon class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Strikethrough"
        @click="editor.chain().focus().toggleStrike().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('strike') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <StrikeIcon class="h-4 w-4" />
      </button>

      <span class="w-px h-4 bg-gray-300 mx-1"></span>

      <!-- Blockquote -->
      <button
        type="button"
        title="Blockquote"
        @click="editor.chain().focus().toggleBlockquote().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('blockquote') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <Quote class="h-4 w-4" />
      </button>

      <!-- Lists -->
      <button
        type="button"
        title="Bullet List"
        @click="editor.chain().focus().toggleBulletList().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('bulletList') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <List class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Numbered List"
        @click="editor.chain().focus().toggleOrderedList().run()"
        :class="[
          'p-1.5 rounded-md hover:bg-gray-200 transition-colors',
          editor.isActive('orderedList') ? 'bg-indigo-100 text-indigo-700 font-bold' : '',
        ]"
      >
        <ListOrdered class="h-4 w-4" />
      </button>

      <span class="w-px h-4 bg-gray-300 mx-1"></span>

      <!-- Undo / Redo -->
      <button
        type="button"
        title="Undo (Ctrl+Z)"
        :disabled="!editor.can().undo()"
        @click="editor.chain().focus().undo().run()"
        class="p-1.5 rounded-md hover:bg-gray-200 disabled:opacity-40 transition-colors"
      >
        <Undo2 class="h-4 w-4" />
      </button>

      <button
        type="button"
        title="Redo (Ctrl+Y)"
        :disabled="!editor.can().redo()"
        @click="editor.chain().focus().redo().run()"
        class="p-1.5 rounded-md hover:bg-gray-200 disabled:opacity-40 transition-colors"
      >
        <Redo2 class="h-4 w-4" />
      </button>
    </div>

    <!-- Editor Body -->
    <EditorContent :editor="editor" />
  </div>
</template>

<style>
/* Prose styling for the rich text editor content */
.tiptap-content h1 {
  font-size: 1.45rem;
  font-weight: 700;
  margin-top: 0.8rem;
  margin-bottom: 0.4rem;
  color: #111827;
}

.tiptap-content h2 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-top: 0.65rem;
  margin-bottom: 0.35rem;
  color: #1f2937;
}

.tiptap-content h3 {
  font-size: 1.1rem;
  font-weight: 600;
  margin-top: 0.5rem;
  margin-bottom: 0.25rem;
  color: #374151;
}

.tiptap-content p {
  margin: 0.35rem 0;
  line-height: 1.6;
}

.tiptap-content ul {
  list-style-type: disc !important;
  padding-left: 1.5rem !important;
  margin: 0.5rem 0 !important;
}

.tiptap-content ol {
  list-style-type: decimal !important;
  padding-left: 1.5rem !important;
  margin: 0.5rem 0 !important;
}

.tiptap-content li {
  margin: 0.25rem 0;
}

.tiptap-content blockquote {
  border-left: 3px solid #6366f1;
  padding-left: 0.85rem;
  margin: 0.6rem 0;
  color: #4b5563;
  font-style: italic;
}

.tiptap-content s {
  text-decoration: line-through;
}

.tiptap-content u {
  text-decoration: underline;
}

/* Also provide a reusable preview class for rendering in JobDetail */
.rich-text-preview h1 {
  font-size: 1.45rem;
  font-weight: 700;
  margin-top: 0.8rem;
  margin-bottom: 0.4rem;
  color: #111827;
}

.rich-text-preview h2 {
  font-size: 1.25rem;
  font-weight: 600;
  margin-top: 0.65rem;
  margin-bottom: 0.35rem;
  color: #1f2937;
}

.rich-text-preview h3 {
  font-size: 1.1rem;
  font-weight: 600;
  margin-top: 0.5rem;
  margin-bottom: 0.25rem;
  color: #374151;
}

.rich-text-preview p {
  margin: 0.35rem 0;
  line-height: 1.6;
}

.rich-text-preview ul {
  list-style-type: disc !important;
  padding-left: 1.5rem !important;
  margin: 0.5rem 0 !important;
}

.rich-text-preview ol {
  list-style-type: decimal !important;
  padding-left: 1.5rem !important;
  margin: 0.5rem 0 !important;
}

.rich-text-preview li {
  margin: 0.25rem 0;
}

.rich-text-preview blockquote {
  border-left: 3px solid #6366f1;
  padding-left: 0.85rem;
  margin: 0.6rem 0;
  color: #4b5563;
  font-style: italic;
}
</style>
