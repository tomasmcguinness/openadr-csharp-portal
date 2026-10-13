import './App.css'
import { AboutPage } from './AboutPage'
import { HomePage } from './HomePage'
import { NavBar } from './NavBar'
import { useRoute } from './useRoute'

function App() {
  const { path, navigate } = useRoute()

  return (
    <div className="page">
      <NavBar path={path} navigate={navigate} />
      {path === '/about' ? <AboutPage /> : <HomePage />}
    </div>
  )
}

export default App
