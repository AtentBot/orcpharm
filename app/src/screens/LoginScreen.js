import React, { useState, useEffect } from 'react';
import {
  View, Text, TextInput, TouchableOpacity, StyleSheet,
  KeyboardAvoidingView, Platform, ScrollView, Alert, ActivityIndicator,
} from 'react-native';
import { LinearGradient } from 'expo-linear-gradient';
import { Feather } from '@expo/vector-icons';
import { useAuth } from '../hooks/useAuth';
import { useBiometrics } from '../hooks/useBiometrics';
import { formatCPF, isValidCPF } from '../utils/formatters';
import { COLORS, GRADIENTS, SPACING, BORDER_RADIUS, FONT_SIZES, SHADOWS } from '../constants/theme';

const LoginScreen = ({ navigation }) => {
  const { login, loginWithBiometrics } = useAuth();
  const { canUseBiometrics, authenticate, getBiometricLabel, getBiometricIcon } = useBiometrics();

  const [cpf, setCpf] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [biometricsAvailable, setBiometricsAvailable] = useState(false);

  useEffect(() => {
    checkBiometrics();
  }, []);

  const checkBiometrics = async () => {
    const canUse = await canUseBiometrics();
    setBiometricsAvailable(canUse);
    if (canUse) handleBiometricLogin();
  };

  const handleBiometricLogin = async () => {
    const authResult = await authenticate({ promptMessage: `Entrar com ${getBiometricLabel()}` });
    if (authResult.success) {
      setLoading(true);
      const result = await loginWithBiometrics();
      setLoading(false);
      if (!result.success) Alert.alert('Sessão expirada', 'Por favor, faça login novamente.');
    }
  };

  const handleLogin = async () => {
    if (!cpf || !password) {
      Alert.alert('Atenção', 'Preencha todos os campos.');
      return;
    }
    if (!isValidCPF(cpf)) {
      Alert.alert('CPF inválido', 'Verifique o CPF digitado.');
      return;
    }

    setLoading(true);
    try {
      const result = await login(cpf, password);
      if (result.success) {
        if (result.requiresVerification) navigation.navigate('VerifyCode');
      } else {
        Alert.alert('Erro', result.message || 'CPF ou senha incorretos.');
      }
    } catch (error) {
      Alert.alert('Erro', 'Não foi possível conectar.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <View style={styles.container}>
      {/* Dark teal header */}
      <LinearGradient colors={GRADIENTS.splash} style={styles.headerGradient}>
        <View style={styles.logoContainer}>
          <View style={styles.logoCircle}>
            <Text style={styles.logoEmoji}>🧪</Text>
          </View>
          <Text style={styles.logoText}>Formula Clear</Text>
          <Text style={styles.subtitle}>Sua farmácia de manipulação</Text>
        </View>
      </LinearGradient>

      {/* Form card overlapping header */}
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        style={styles.formWrapper}
      >
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
          showsVerticalScrollIndicator={false}
        >
          <View style={styles.formCard}>
            <Text style={styles.welcomeText}>Bem-vindo de volta!</Text>
            <Text style={styles.instructionText}>Entre com seu CPF e senha</Text>

            {/* CPF */}
            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="user" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="CPF"
                placeholderTextColor={COLORS.textMuted}
                value={cpf}
                onChangeText={(t) => setCpf(formatCPF(t))}
                keyboardType="numeric"
                maxLength={14}
                editable={!loading}
              />
            </View>

            {/* Password */}
            <View style={styles.inputContainer}>
              <View style={styles.inputAccent} />
              <Feather name="lock" size={20} color={COLORS.textMuted} style={styles.inputIcon} />
              <TextInput
                style={styles.input}
                placeholder="Senha"
                placeholderTextColor={COLORS.textMuted}
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!showPassword}
                editable={!loading}
              />
              <TouchableOpacity onPress={() => setShowPassword(!showPassword)} style={styles.eyeButton}>
                <Feather name={showPassword ? 'eye-off' : 'eye'} size={20} color={COLORS.textMuted} />
              </TouchableOpacity>
            </View>

            {/* Login Button */}
            <TouchableOpacity onPress={handleLogin} disabled={loading} activeOpacity={0.8}>
              <View style={styles.loginButtonShadow}>
                <LinearGradient
                  colors={GRADIENTS.primary}
                  start={{ x: 0, y: 0 }}
                  end={{ x: 1, y: 0 }}
                  style={styles.loginButton}
                >
                  {loading ? (
                    <ActivityIndicator color={COLORS.white} />
                  ) : (
                    <>
                      <Text style={styles.loginButtonText}>Entrar</Text>
                      <Feather name="arrow-right" size={20} color={COLORS.white} />
                    </>
                  )}
                </LinearGradient>
              </View>
            </TouchableOpacity>

            {/* Biometrics */}
            {biometricsAvailable && (
              <TouchableOpacity onPress={handleBiometricLogin} style={styles.biometricButton} disabled={loading}>
                <Text style={styles.biometricIcon}>{getBiometricIcon()}</Text>
                <Text style={styles.biometricText}>Entrar com {getBiometricLabel()}</Text>
              </TouchableOpacity>
            )}

            {/* Divider */}
            <View style={styles.divider}>
              <View style={styles.dividerLine} />
              <Text style={styles.dividerText}>ou</Text>
              <View style={styles.dividerLine} />
            </View>

            {/* Register */}
            <TouchableOpacity onPress={() => navigation.navigate('Register')} activeOpacity={0.7}>
              <LinearGradient
                colors={[COLORS.primaryMuted, 'rgba(13, 148, 136, 0.04)']}
                style={styles.registerButton}
              >
                <Text style={styles.registerText}>Não tem conta?</Text>
                <Text style={styles.registerTextBold}>Cadastre-se</Text>
                <Feather name="chevron-right" size={18} color={COLORS.primary} />
              </LinearGradient>
            </TouchableOpacity>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.background,
  },
  headerGradient: {
    height: '42%',
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: Platform.OS === 'ios' ? 60 : 40,
  },
  logoContainer: {
    alignItems: 'center',
  },
  logoCircle: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: 'rgba(255,255,255,0.15)',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: SPACING.lg,
  },
  logoEmoji: {
    fontSize: 40,
  },
  logoText: {
    fontSize: FONT_SIZES.hero,
    fontWeight: '800',
    color: COLORS.white,
    letterSpacing: -0.5,
  },
  subtitle: {
    fontSize: FONT_SIZES.md,
    color: 'rgba(255,255,255,0.7)',
    marginTop: SPACING.xs,
  },
  formWrapper: {
    flex: 1,
    marginTop: -60,
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: SPACING.xl,
    paddingBottom: SPACING.xxxl,
  },
  formCard: {
    backgroundColor: COLORS.white,
    borderRadius: BORDER_RADIUS.xxl,
    padding: SPACING.xxl,
    ...SHADOWS.large,
  },
  welcomeText: {
    fontSize: FONT_SIZES.xxl,
    fontWeight: '700',
    color: COLORS.text,
    textAlign: 'center',
  },
  instructionText: {
    fontSize: FONT_SIZES.md,
    color: COLORS.textSecondary,
    textAlign: 'center',
    marginTop: SPACING.xs,
    marginBottom: SPACING.xxl,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: COLORS.backgroundLight,
    borderRadius: BORDER_RADIUS.md,
    marginBottom: SPACING.lg,
    overflow: 'hidden',
  },
  inputAccent: {
    width: 3,
    alignSelf: 'stretch',
    backgroundColor: COLORS.primary,
    borderTopLeftRadius: BORDER_RADIUS.md,
    borderBottomLeftRadius: BORDER_RADIUS.md,
  },
  inputIcon: {
    paddingHorizontal: SPACING.md,
  },
  input: {
    flex: 1,
    paddingVertical: SPACING.lg,
    fontSize: FONT_SIZES.md,
    color: COLORS.text,
  },
  eyeButton: {
    padding: SPACING.md,
  },
  loginButtonShadow: {
    marginTop: SPACING.lg,
    borderRadius: BORDER_RADIUS.md,
    ...SHADOWS.colored(COLORS.primary),
  },
  loginButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: SPACING.lg,
    borderRadius: BORDER_RADIUS.md,
    gap: SPACING.sm,
  },
  loginButtonText: {
    fontSize: FONT_SIZES.lg,
    fontWeight: '700',
    color: COLORS.white,
    letterSpacing: 0.3,
  },
  biometricButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    alignSelf: 'center',
    paddingVertical: SPACING.md,
    paddingHorizontal: SPACING.xxl,
    marginTop: SPACING.lg,
    borderRadius: BORDER_RADIUS.full,
    backgroundColor: COLORS.primaryMuted,
    gap: SPACING.sm,
  },
  biometricIcon: {
    fontSize: 22,
  },
  biometricText: {
    fontSize: FONT_SIZES.md,
    color: COLORS.primary,
    fontWeight: '600',
  },
  divider: {
    flexDirection: 'row',
    alignItems: 'center',
    marginVertical: SPACING.xl,
  },
  dividerLine: {
    flex: 1,
    height: 1,
    backgroundColor: COLORS.border,
  },
  dividerText: {
    marginHorizontal: SPACING.md,
    fontSize: FONT_SIZES.sm,
    color: COLORS.textMuted,
  },
  registerButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: SPACING.lg,
    borderRadius: BORDER_RADIUS.md,
    gap: SPACING.xs,
  },
  registerText: {
    fontSize: FONT_SIZES.md,
    color: COLORS.textSecondary,
  },
  registerTextBold: {
    fontSize: FONT_SIZES.md,
    color: COLORS.primary,
    fontWeight: '700',
  },
});

export default LoginScreen;
