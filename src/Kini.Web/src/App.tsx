import { Routes, Route } from 'react-router'
import { Landing } from './pages/Landing'
import { SignIn } from './pages/SignIn'
import { SignUp } from './pages/SignUp'
import { AppShell } from './components/AppShell'
import { Dashboard } from './pages/Dashboard'
import { Keys } from './pages/Keys'
import { Identities } from './pages/Identities'
import { Tokens } from './pages/Tokens'

export function App() {
  return (
    <Routes>
      <Route path="/" element={<Landing />} />
      <Route path="/sign-in" element={<SignIn />} />
      <Route path="/sign-up" element={<SignUp />} />

      <Route path="/app" element={<AppShell />}>
        <Route index element={<Dashboard />} />
        <Route path="identities" element={<Identities />} />
        <Route path="keys" element={<Keys />} />
        <Route path="tokens" element={<Tokens />} />
      </Route>
    </Routes>
  )
}
