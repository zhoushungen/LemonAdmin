<template>
  <LemonPageHeader title="审计日志" description="记录真实操作者、当前身份、请求耗时、TraceId 和来源 IP" />
  <LemonSearchForm @search="load" @reset="reset">
    <el-input v-model="query.keyword" placeholder="路径 / TraceId / IP" clearable style="width: 240px" />
    <el-input v-model="query.module" placeholder="模块" clearable style="width: 160px" />
  </LemonSearchForm>
  <div class="page-card">
    <LemonTable table-key="system-audit-logs" export-name="审计日志" :rows="rows" :columns="columns" :loading="loading" :pagination="page" @refresh="load" @page-change="changePage">
      <template #cell-isImpersonating="{ row }"><el-tag v-if="row.isImpersonating" type="warning">账号切换</el-tag><span v-else>正常</span></template>
      <template #cell-statusCode="{ row }"><el-tag :type="row.statusCode < 400 ? 'success' : 'danger'">{{ row.statusCode }}</el-tag></template>
    </LemonTable>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import LemonPageHeader from '@/components/lemon/LemonPageHeader.vue'
import LemonSearchForm from '@/components/lemon/LemonSearchForm.vue'
import LemonTable from '@/components/lemon/LemonTable.vue'
import { fetchAuditLogs } from '@/api/system'
import type { AuditLog } from '@/types/api'
import type { LemonColumn } from '@/types/table'

const rows = ref<AuditLog[]>([])
const loading = ref(false)
const query = reactive({ keyword: '', module: '' })
const page = reactive({ pageIndex: 1, pageSize: 20, total: 0 })
const columns: LemonColumn<AuditLog>[] = [
  { key: 'createdAt', label: '时间', minWidth: 170, fixed: 'left' },
  { key: 'actorAdminUserId', label: '真实操作者ID' },
  { key: 'adminUserId', label: '当前身份ID' },
  { key: 'departmentId', label: '部门ID' },
  { key: 'isImpersonating', label: '身份状态' },
  { key: 'module', label: '模块' },
  { key: 'action', label: '动作' },
  { key: 'httpMethod', label: '方法' },
  { key: 'requestPath', label: '请求路径', minWidth: 260 },
  { key: 'statusCode', label: '状态码' },
  { key: 'ipAddress', label: 'IP' },
  { key: 'elapsedMilliseconds', label: '耗时(ms)' },
  { key: 'traceId', label: 'TraceId', minWidth: 260 }
]
async function load() { loading.value = true; try { const result = await fetchAuditLogs({ ...query, pageIndex: page.pageIndex, pageSize: page.pageSize }); rows.value = result.items; page.total = result.total } finally { loading.value = false } }
function reset() { query.keyword = ''; query.module = ''; page.pageIndex = 1; load() }
function changePage(value: any) { page.pageIndex = value.pageIndex; page.pageSize = value.pageSize; load() }
onMounted(load)
</script>
