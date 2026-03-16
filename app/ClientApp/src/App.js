import { Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { ViewReplay } from './components/ViewReplay';

import './custom.css'

export default function App() {
  return (
    <Layout>
      <Routes>
        <Route path='/' element={<Home />} />
        <Route path='/view/:guid' element={<ViewReplay />} />
      </Routes>
    </Layout>
  );
}
