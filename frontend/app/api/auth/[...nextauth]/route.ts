import NextAuth from 'next-auth';
import OAuthProvider from 'next-auth/providers/oauth';
import CredentialsProvider from 'next-auth/providers/credentials';

const issuer = process.env.SSO_ISSUER;
const clientId = process.env.SSO_CLIENT_ID;
const clientSecret = process.env.SSO_CLIENT_SECRET;
const scope = process.env.SSO_SCOPE ?? 'openid profile email offline_access roles';
const allowDevCredentials = process.env.AUTH_ALLOW_DEV === 'true';

const providers = [] as any[];

if (issuer && clientId && clientSecret) {
  providers.push(
    OAuthProvider({
      id: 'nabteams-sso',
      name: 'NabTeams SSO',
      clientId,
      clientSecret,
      issuer,
      authorization: { params: { scope } }
    })
  );
}

if (providers.length === 0 || allowDevCredentials) {
  providers.push(
    CredentialsProvider({
      id: 'dev-login',
      name: 'ورود آزمایشی',
      credentials: {
        email: { label: 'ایمیل', type: 'email' },
        role: { label: 'نقش', type: 'text', placeholder: 'participant' }
      },
      authorize(credentials) {
        if (!credentials?.email) {
          return null;
        }
        const role = (credentials.role ?? 'participant').toString().toLowerCase();
        return {
          id: credentials.email,
          name: credentials.email,
          email: credentials.email,
          roles: [role]
        } as any;
      }
    })
  );
}

if (providers.length === 0) {
  throw new Error('هیچ ارائه‌دهنده احراز هویتی پیکربندی نشده است. متغیرهای محیطی SSO را تنظیم کنید.');
}

const handler = NextAuth({
  providers,
  session: { strategy: 'jwt' },
  pages: { signIn: '/auth/signin' },
  callbacks: {
    async jwt({ token, account, profile, user }) {
      if (account) {
        token.accessToken = (account as any).access_token ?? (account as any).id_token ?? token.accessToken;
      }
      if (user && (user as any).roles) {
        token.roles = (user as any).roles as string[];
      } else if (profile && (profile as any).roles) {
        const roles = Array.isArray((profile as any).roles)
          ? ((profile as any).roles as string[])
          : [String((profile as any).roles)];
        token.roles = roles;
      }
      return token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken as string | undefined;
      session.user = {
        ...session.user,
        id: token.sub,
        roles: (token.roles as string[] | undefined) ?? session.user?.roles ?? []
      };
      return session;
    }
  }
});

export { handler as GET, handler as POST };
