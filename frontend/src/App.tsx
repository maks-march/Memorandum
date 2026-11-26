import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout/AppLayout';
import { AuthLayout } from '@/components/layout/AuthLayout/AuthLayout';
import { HomePage } from '@/pages/HomePage/HomePage';
import { LoginPage } from '@/pages/LoginPage/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage/RegisterPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage/ForgotPasswordPage';
import { ResetPasswordPage } from '@/pages/ResetPasswordPage/ResetPasswordPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Главная страница с хейдером по макету Figma */}
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
        </Route>

        {/* Экраны авторизации и восстановления доступа (split-макет без хедера) */}
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
        </Route>

        {/* Фоллбек для любых других путей */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
