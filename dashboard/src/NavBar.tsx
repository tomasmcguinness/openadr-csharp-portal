import { NavLink } from './useRoute'

export function NavBar({ path, navigate }: { path: string; navigate: (to: string) => void }) {
  return (
    <nav className="nav-bar">
      <NavLink to="/" navigate={navigate} className={path === '/' ? 'nav-link nav-link-active' : 'nav-link'}>
        My Home
      </NavLink>
      <NavLink to="/about" navigate={navigate} className={path === '/about' ? 'nav-link nav-link-active' : 'nav-link'}>
        About
      </NavLink>
    </nav>
  )
}
