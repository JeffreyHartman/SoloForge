import { createApp } from 'vue'
import './style.css'
import App from './App.vue'

// Initialize theme from localStorage before mounting (prevents flash)
const stored = localStorage.getItem('soloforge-theme')
if (stored === 'light') {
  document.documentElement.classList.remove('dark')
} else {
  // Default to dark mode
  document.documentElement.classList.add('dark')
}

createApp(App).mount('#app')
