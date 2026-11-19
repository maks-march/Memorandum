// frontend/src/App.tsx
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout/AppLayout';
import { HomePage } from '@/pages/HomePage/HomePage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Главная страница с хейдером по макету Figma */}
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
        </Route>

        {/* Фоллбек для любых других путей */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
