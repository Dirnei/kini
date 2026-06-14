import { Routes, Route } from 'react-router'
import { Landing } from './pages/Landing'
import { SignIn } from './pages/SignIn'
import { Dashboard } from './pages/Dashboard'

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/sign-in" element={<SignIn />} />
      <Route path="/app" element={<Dashboard />} />
    </Routes>
  )
}
