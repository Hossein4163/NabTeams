import NextAuth, { DefaultSession } from 'next-auth';
import { JWT } from 'next-auth/jwt';

declare module 'next-auth' {
  interface Session {
    accessToken?: string;
    user?: DefaultSession['user'] & {
      id?: string;
      roles?: string[];
    };
  }
}

declare module 'next-auth/jwt' {
  interface JWT {
    accessToken?: string;
    roles?: string[];
  }
}

export {};
