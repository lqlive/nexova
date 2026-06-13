// Local mock user service.
// The backend API has been removed for this BI demo, so all auth state is
// kept in localStorage and no network requests are made.

// Login request interface
export interface LoginRequest {
  email: string;
  password: string;
}

// Login response interface
export interface LoginResponse {
  id: string;
  name: string;
  email: string;
  avatar?: string;
  provider: string;
}

// User update request interface
export interface UserUpdateRequest {
  name?: string;
  email?: string;
  avatar?: string;
  firstName?: string;
  lastName?: string;
  bio?: string;
}

const STORAGE_KEY = 'superset_demo_user';

const readStoredUser = (): LoginResponse | null => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as LoginResponse) : null;
  } catch {
    return null;
  }
};

const writeStoredUser = (user: LoginResponse): void => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
};

const nameFromEmail = (email: string): string => {
  const local = email.split('@')[0] || 'user';
  return local
    .split(/[._-]+/)
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1))
    .join(' ');
};

const buildUser = (email: string, provider = 'local'): LoginResponse => ({
  id: '1',
  name: nameFromEmail(email),
  email,
  provider,
});

// User service class (mock implementation)
export class UserService {
  /** User login - accepts any credentials and stores a local mock user. */
  static async login(loginData: LoginRequest): Promise<void> {
    writeStoredUser(buildUser(loginData.email));
  }

  /** User registration - stores a local mock user. */
  static async register(registerData: {
    email: string;
    password: string;
    name: string;
  }): Promise<void> {
    writeStoredUser({
      ...buildUser(registerData.email),
      name: registerData.name || nameFromEmail(registerData.email),
    });
  }

  /** Get current user from localStorage; rejects when not logged in. */
  static async getCurrentUser(): Promise<LoginResponse> {
    const user = readStoredUser();
    if (!user) {
      throw new Error('Not authenticated');
    }
    return user;
  }

  /** Clear local auth state. */
  static async logout(): Promise<void> {
    localStorage.removeItem(STORAGE_KEY);
  }

  /** Update the stored user. */
  static async updateUser(updateData: UserUpdateRequest): Promise<LoginResponse> {
    const current = readStoredUser() ?? buildUser('demo@superset.io');
    const updated: LoginResponse = {
      ...current,
      ...(updateData.name ? { name: updateData.name } : {}),
      ...(updateData.email ? { email: updateData.email } : {}),
      ...(updateData.avatar ? { avatar: updateData.avatar } : {}),
    };
    writeStoredUser(updated);
    return updated;
  }

  /** Mock provider login - stores a user then redirects back. */
  static loginWithProvider(providerId: string, redirectUri?: string): void {
    writeStoredUser(buildUser(`${providerId.toLowerCase()}@superset.io`, providerId));
    window.location.href = redirectUri || window.location.origin + '/';
  }
}
