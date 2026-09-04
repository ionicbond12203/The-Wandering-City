import type { Metadata } from 'next';
import './globals.css';
export const metadata: Metadata = {
  title: '逐风之旅 · The Wandering City',
  description: '在风起之地探索、战斗、制作与建造。开放世界冒险交互体验。',
};
export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="zh-CN">
      <body>{children}</body>
    </html>
  );
}
