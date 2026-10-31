import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { Toaster } from 'vue-sonner'
import { useAuthStore } from './stores/auth'
import { useTheme } from './composables/useTheme'
import './assets/main.css'

// Initialize theme immediately to prevent theme flash
const { initTheme } = useTheme()
initTheme()

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

app.component('Toaster', Toaster)

const auth = useAuthStore()
auth.init().then(() => {
    app.mount('#app')
})