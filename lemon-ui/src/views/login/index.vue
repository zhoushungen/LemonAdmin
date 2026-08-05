<template>
  <div class="login-page">
    <div class="login-card">
      <div class="brand">
        <div class="logo">L</div>
        <div>
          <h1>Lemon</h1>
          <p>轻量通用管理框架</p>
        </div>
      </div>

      <el-form :model="form" @keyup.enter="submit">
        <el-form-item>
          <el-input v-model="form.username" size="large" placeholder="用户名" prefix-icon="User" />
        </el-form-item>
        <el-form-item>
          <el-input
            v-model="form.password"
            type="password"
            show-password
            size="large"
            placeholder="密码"
            prefix-icon="Lock"
          />
        </el-form-item>
        <el-button class="w-full" size="large" type="primary" :loading="loading" @click="submit">
          登录
        </el-button>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const form = reactive({ username: '', password: '' })
const loading = ref(false)
const router = useRouter()
const auth = useAuthStore()

async function submit() {
  loading.value = true
  try {
    await auth.login(form.username, form.password)
    await router.push('/dashboard')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page { min-height: 100vh; display: grid; place-items: center; background: #f4f7fb; }
.login-card { width: 380px; padding: 36px; border-radius: 18px; background: #fff; box-shadow: 0 18px 50px rgb(15 23 42 / 10%); }
.brand { display: flex; align-items: center; gap: 14px; margin-bottom: 28px; }
.brand h1, .brand p { margin: 0; }
.brand p { margin-top: 4px; color: #94a3b8; }
.logo { display: grid; width: 48px; height: 48px; place-items: center; border-radius: 14px; background: var(--lemon-primary); color: #fff; font-size: 24px; font-weight: 700; }
.w-full { width: 100%; }
</style>
