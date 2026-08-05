<template><div class="page-card search"><slot/><div class="actions"><el-button type="primary" @click="$emit('search')">查询</el-button><el-button @click="$emit('reset')">重置</el-button></div></div></template>
<script setup lang="ts">defineEmits<{search:[];reset:[]}>()</script>
<style scoped>.search{display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin-bottom:14px}.actions{margin-left:auto;display:flex;gap:8px}</style>
