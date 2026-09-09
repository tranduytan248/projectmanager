<#
.SYNOPSIS
    Kiểm tra và đồng bộ dữ liệu mới/thay đổi từ CSDL Trung tâm (10.57.30.10) sang CSDL Production Viễn thông (10.57.47.2\MSSQL2012).

.DESCRIPTION
    - Chế độ -CheckOnly (mặc định nếu không truyền -Apply): So sánh schema và dữ liệu, liệt kê các bảng có bản ghi mới hoặc thay đổi mà không tác động CSDL.
    - Chế độ -Apply: Đồng bộ các cột schema mới (nếu có), nạp bản ghi mới (giữ nguyên Identity ID) và cập nhật các bản ghi có dữ liệu mới hơn (dựa vào UpdatedAt).

.PARAMETER Apply
    Nếu có switch này, script sẽ thực hiện đồng bộ thực tế vào CSDL Target.

.PARAMETER SourceConnStr
    Chuỗi kết nối tới CSDL Trung tâm.

.PARAMETER TargetConnStr
    Chuỗi kết nối tới CSDL Production Viễn thông.
#>
[CmdletBinding()]
param(
    [switch]$Apply,
    [string]$SourceConnStr = "Server=10.57.30.10,1433;Database=pmncpt.cenit.vn;User Id=pmncpt.cenit.vn;Password=W6!DTCPk@QJ6k3;TrustServerCertificate=True;Connect Timeout=15;",
    [string]$TargetConnStr = "Server=10.57.47.2\MSSQL2012;Database=pmncpt.cenit.vn;User Id=tan.td;Password=Tdtan@123;TrustServerCertificate=True;Connect Timeout=15;"
)

$ErrorActionPreference = "Stop"

$modeText = if ($Apply) { "THUC THI DONG BO (-Apply)" } else { "CHI KIEM TRA (-CheckOnly)" }
$modeColor = if ($Apply) { "Yellow" } else { "Green" }

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " DATABASE SYNC: pmncpt.cenit.vn (30.10) -> brewtask (47.2)" -ForegroundColor Cyan
Write-Host " Che do: $modeText" -ForegroundColor $modeColor
Write-Host "=================================================================" -ForegroundColor Cyan

$sConn = New-Object System.Data.SqlClient.SqlConnection($SourceConnStr)
$tConn = New-Object System.Data.SqlClient.SqlConnection($TargetConnStr)

try {
    $sConn.Open()
    $tConn.Open()
    Write-Host "[OK] Da ket noi thanh cong toi ca 2 may chu CSDL!" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Loi ket noi CSDL: $($_.Exception.Message)" -ForegroundColor Red
    if ($sConn.State -eq 'Open') { $sConn.Close() }
    if ($tConn.State -eq 'Open') { $tConn.Close() }
    exit 1
}

try {
    # -------------------------------------------------------------
    # 1. KIEM TRA SCHEMA (BANG & COT)
    # -------------------------------------------------------------
    Write-Host ""
    Write-Host "--- BUOC 1: KIEM TRA SCHEMA (CAU TRUC BANG & COT) ---" -ForegroundColor Cyan

    $cmdCols = $sConn.CreateCommand()
    $cmdCols.CommandText = @"
SELECT c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.IS_NULLABLE, c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
INNER JOIN INFORMATION_SCHEMA.TABLES t ON c.TABLE_NAME = t.TABLE_NAME AND t.TABLE_TYPE = 'BASE TABLE'
WHERE c.TABLE_SCHEMA = 'dbo'
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION
"@
    $r = $cmdCols.ExecuteReader()
    $sCols = @{}
    while ($r.Read()) {
        $t = $r.GetString(0)
        $c = $r.GetString(1)
        $sCols["$t.$c"] = @{
            Table = $t
            Column = $c
            Type = $r.GetString(2)
            Nullable = $r.GetString(3)
            MaxLen = if ($r.IsDBNull(4)) { $null } else { $r.GetValue(4) }
        }
    }
    $r.Close()

    $cmdTCols = $tConn.CreateCommand()
    $cmdTCols.CommandText = @"
SELECT TABLE_NAME, COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
"@
    $rT = $cmdTCols.ExecuteReader()
    $tCols = New-Object 'System.Collections.Generic.HashSet[string]'
    while ($rT.Read()) {
        [void]$tCols.Add("$($rT.GetString(0)).$($rT.GetString(1))")
    }
    $rT.Close()

    $missingCols = @()
    foreach ($k in $sCols.Keys) {
        if (-not $tCols.Contains($k)) {
            $missingCols += $sCols[$k]
        }
    }

    if ($missingCols.Count -eq 0) {
        Write-Host " [PASS] Cau truc bang va cot hoan toan khop (0 cot thieu)." -ForegroundColor Green
    } else {
        Write-Host " [WARN] Phat hien $($missingCols.Count) cot thieu tren Target (47.2):" -ForegroundColor Yellow
        foreach ($m in $missingCols) {
            Write-Host "   - Bang [$($m.Table)] -> Cot [$($m.Column)] ($($m.Type))" -ForegroundColor Yellow
            if ($Apply) {
                # Tu dong them cot moi
                $typeDef = $m.Type
                if ($m.MaxLen) {
                    $typeDef += "($($m.MaxLen))"
                } elseif ($m.Type -in 'varchar', 'nvarchar', 'varbinary') {
                    $typeDef += "(MAX)"
                }
                $nullDef = "NULL"
                $alterSql = "ALTER TABLE [dbo].[$($m.Table)] ADD [$($m.Column)] $typeDef $nullDef;"
                Write-Host "     => Thuc thi: $alterSql" -ForegroundColor Cyan
                $cmdAlter = $tConn.CreateCommand()
                $cmdAlter.CommandText = $alterSql
                $cmdAlter.ExecuteNonQuery() | Out-Null
                Write-Host "     => Them cot [$($m.Column)] thanh cong!" -ForegroundColor Green
            }
        }
    }

    # -------------------------------------------------------------
    # 2. KIEM TRA VA TONG HOP DU LIEU CHENH LECH
    # -------------------------------------------------------------
    Write-Host ""
    Write-Host "--- BUOC 2: SO SANH DU LIEU GIUA 2 CSDL ---" -ForegroundColor Cyan

    $cmdTables = $sConn.CreateCommand()
    $cmdTables.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME"
    $rTables = $cmdTables.ExecuteReader()
    $allTables = New-Object System.Collections.Generic.List[string]
    while ($rTables.Read()) { [void]$allTables.Add($rTables.GetString(0)) }
    $rTables.Close()

    # Thu tu uu tien theo rang buoc khoa ngoai
    $priorityTables = @(
        'WorkProjects',
        'WorkTasks',
        'WorkAssignments',
        'WorkComments',
        'WorkTimeLogs',
        'TaskActivityLogs',
        'UserNotifications',
        'LeaveRequests',
        'ReminderLogs'
    )
    $sortedTables = New-Object System.Collections.Generic.List[string]
    foreach ($pt in $priorityTables) {
        if ($allTables.Contains($pt)) { [void]$sortedTables.Add($pt) }
    }
    foreach ($t in $allTables) {
        if (-not $sortedTables.Contains($t)) { [void]$sortedTables.Add($t) }
    }

    $diffReport = @()

    foreach ($tbl in $sortedTables) {
        $cmdSCount = $sConn.CreateCommand()
        $cmdSCount.CommandText = "SELECT COUNT(*) FROM [$tbl]"
        $sCount = [int]$cmdSCount.ExecuteScalar()

        $cmdTCount = $tConn.CreateCommand()
        $cmdTCount.CommandText = "IF OBJECT_ID('[$tbl]', 'U') IS NOT NULL SELECT COUNT(*) FROM [$tbl] ELSE SELECT -1"
        $tCount = [int]$cmdTCount.ExecuteScalar()

        # Kiem tra PK & Identity
        $cmdPK = $sConn.CreateCommand()
        $cmdPK.CommandText = @"
SELECT col.name, col.is_identity
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns col ON ic.object_id = col.object_id AND ic.column_id = col.column_id
WHERE i.object_id = OBJECT_ID('[$tbl]') AND i.is_primary_key = 1
"@
        $rPK = $cmdPK.ExecuteReader()
        $pkCols = @()
        $hasIdentity = $false
        while ($rPK.Read()) {
            $pkCols += $rPK.GetString(0)
            if ($rPK.GetBoolean(1)) { $hasIdentity = $true }
        }
        $rPK.Close()

        # Kiem tra cot UpdatedAt
        $cmdUpdCol = $sConn.CreateCommand()
        $cmdUpdCol.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '$tbl' AND COLUMN_NAME = 'UpdatedAt'"
        $hasUpdatedAt = ([int]$cmdUpdCol.ExecuteScalar() -gt 0)

        # Dem ban ghi co UpdatedAt moi hon
        $updatedDiff = 0
        if ($hasUpdatedAt -and $pkCols -contains 'Id' -and $tCount -gt 0) {
            $cmdCheckUpd = $sConn.CreateCommand()
            $cmdCheckUpd.CommandText = "SELECT Id, UpdatedAt FROM [$tbl] WHERE UpdatedAt IS NOT NULL"
            $rUpd = $cmdCheckUpd.ExecuteReader()
            $sTimes = @{}
            while ($rUpd.Read()) { $sTimes[$rUpd.GetInt32(0)] = $rUpd.GetDateTime(1) }
            $rUpd.Close()

            $cmdCheckTUpd = $tConn.CreateCommand()
            $cmdCheckTUpd.CommandText = "SELECT Id, UpdatedAt FROM [$tbl] WHERE UpdatedAt IS NOT NULL"
            $rTUpd = $cmdCheckTUpd.ExecuteReader()
            $tTimes = @{}
            while ($rTUpd.Read()) { $tTimes[$rTUpd.GetInt32(0)] = $rTUpd.GetDateTime(1) }
            $rTUpd.Close()

            foreach ($id in $sTimes.Keys) {
                if ($tTimes.ContainsKey($id) -and $sTimes[$id] -gt $tTimes[$id]) {
                    $updatedDiff++
                }
            }
        }

        $rowDiff = $sCount - $tCount
        if ($rowDiff -ne 0 -or $updatedDiff -gt 0) {
            $diffReport += [pscustomobject]@{
                Table = $tbl
                SourceCount = $sCount
                TargetCount = $tCount
                NewRows = if ($rowDiff -gt 0) { $rowDiff } else { 0 }
                UpdatedRows = $updatedDiff
                PK = ($pkCols -join ', ')
                HasIdentity = $hasIdentity
                HasUpdatedAt = $hasUpdatedAt
            }
        }
    }

    Write-Host ""
    $header = "{0,-28} | {1,8} | {2,8} | {3,10} | {4,10} | {5}" -f "Ten bang", "Source", "Target", "Dong moi", "Doi moi", "Khoa chinh"
    Write-Host $header
    Write-Host ("-" * 86)
    if ($diffReport.Count -eq 0) {
        Write-Host "Khong co bang nao co chenh lech! Hai CSDL dong nhat 100%." -ForegroundColor Green
    } else {
        foreach ($d in $diffReport) {
            $newStr = "+$($d.NewRows)"
            $updStr = "$($d.UpdatedRows)"
            $line = "{0,-28} | {1,8} | {2,8} | {3,10} | {4,10} | {5}" -f $d.Table, $d.SourceCount, $d.TargetCount, $newStr, $updStr, $d.PK
            Write-Host $line -ForegroundColor Yellow
        }
    }
    Write-Host ("-" * 86)
    Write-Host "Tong so bang co du lieu moi / thay doi: $($diffReport.Count)"

    # -------------------------------------------------------------
    # 3. THUC THI DONG BO DU LIEU (NEU CO CO -Apply)
    # -------------------------------------------------------------
    if ($Apply -and $diffReport.Count -gt 0) {
        Write-Host ""
        Write-Host "--- BUOC 3: TIEN HANH DONG BO DU LIEU (-Apply) ---" -ForegroundColor Cyan

        # Tat toan bo constraint tren Target
        Write-Host " [1/3] Tam tat khoa ngoai (NOCHECK CONSTRAINT ALL)..." -ForegroundColor Yellow
        $cmdDisableAll = $tConn.CreateCommand()
        $cmdDisableAll.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL;'"
        $cmdDisableAll.ExecuteNonQuery() | Out-Null

        $syncSuccessCount = 0
        $syncFailCount = 0

        foreach ($d in $diffReport) {
            $tbl = $d.Table
            Write-Host " [*] Dang dong bo bang: [$tbl] ..." -ForegroundColor Cyan

            try {
                # 1. Lay danh sach cot tu Source
                $cmdTableCols = $sConn.CreateCommand()
                $cmdTableCols.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '$tbl' AND TABLE_SCHEMA = 'dbo' ORDER BY ORDINAL_POSITION"
                $rCols = $cmdTableCols.ExecuteReader()
                $cols = New-Object System.Collections.Generic.List[string]
                while ($rCols.Read()) { [void]$cols.Add($rCols.GetString(0)) }
                $rCols.Close()

                # 2. Lay du lieu tu Source
                $cmdDataS = $sConn.CreateCommand()
                $cmdDataS.CommandText = "SELECT * FROM [$tbl]"
                $adp = New-Object System.Data.SqlClient.SqlDataAdapter($cmdDataS)
                $dtSrc = New-Object System.Data.DataTable
                $adp.Fill($dtSrc) | Out-Null

                # 3. Lay Id va UpdatedAt tu Target (neu co PK Id)
                $tgtIdSet = New-Object 'System.Collections.Generic.HashSet[int]'
                $tgtUpdMap = @{}
                if ($d.PK -eq 'Id') {
                    $cmdTgtIds = $tConn.CreateCommand()
                    $cmdTgtIds.CommandText = if ($d.HasUpdatedAt) { "SELECT Id, UpdatedAt FROM [$tbl]" } else { "SELECT Id FROM [$tbl]" }
                    $rTgtIds = $cmdTgtIds.ExecuteReader()
                    while ($rTgtIds.Read()) {
                        $id = $rTgtIds.GetInt32(0)
                        [void]$tgtIdSet.Add($id)
                        if ($d.HasUpdatedAt) {
                            $tgtUpdMap[$id] = if ($rTgtIds.IsDBNull(1)) { [datetime]::MinValue } else { $rTgtIds.GetDateTime(1) }
                        }
                    }
                    $rTgtIds.Close()
                }

                # 4. Phan loai dong moi va dong can update
                $dtNew = $dtSrc.Clone()
                $dtUpdate = $dtSrc.Clone()

                foreach ($row in $dtSrc.Rows) {
                    if ($d.PK -eq 'Id') {
                        $id = [int]$row['Id']
                        if (-not $tgtIdSet.Contains($id)) {
                            $dtNew.ImportRow($row)
                        } elseif ($d.HasUpdatedAt) {
                            $srcUpd = if ($row['UpdatedAt'] -is [DBNull]) { [datetime]::MinValue } else { [datetime]$row['UpdatedAt'] }
                            if ($srcUpd -gt $tgtUpdMap[$id]) {
                                $dtUpdate.ImportRow($row)
                            }
                        }
                    } else {
                        if ($d.TargetCount -eq 0) {
                            $dtNew.ImportRow($row)
                        }
                    }
                }

                # 5. Insert dong moi bang SqlBulkCopy
                if ($dtNew.Rows.Count -gt 0) {
                    $bOptions = if ($d.HasIdentity) { 
                        [System.Data.SqlClient.SqlBulkCopyOptions]'KeepIdentity, KeepNulls' 
                    } else { 
                        [System.Data.SqlClient.SqlBulkCopyOptions]'KeepNulls' 
                    }
                    $bulk = New-Object System.Data.SqlClient.SqlBulkCopy($TargetConnStr, $bOptions)
                    $bulk.BulkCopyTimeout = 120
                    $bulk.DestinationTableName = "dbo.[$tbl]"
                    foreach ($c in $cols) {
                        [void]$bulk.ColumnMappings.Add($c, $c)
                    }
                    $bulk.WriteToServer($dtNew)
                    $bulk.Close()
                    Write-Host "   + Da them moi $($dtNew.Rows.Count) ban ghi vao [$tbl]" -ForegroundColor Green
                }

                # 6. Update dong thay doi qua bang tam
                if ($dtUpdate.Rows.Count -gt 0) {
                    $tempTblName = "##Sync_Temp_$tbl"
                    $cmdTemp = $tConn.CreateCommand()
                    $cmdTemp.CommandText = "IF OBJECT_ID('tempdb..$tempTblName') IS NOT NULL DROP TABLE $tempTblName; SELECT TOP 0 * INTO $tempTblName FROM dbo.[$tbl];"
                    $cmdTemp.ExecuteNonQuery() | Out-Null

                    $bulkUpd = New-Object System.Data.SqlClient.SqlBulkCopy($TargetConnStr, [System.Data.SqlClient.SqlBulkCopyOptions]'KeepIdentity, KeepNulls')
                    $bulkUpd.BulkCopyTimeout = 120
                    $bulkUpd.DestinationTableName = $tempTblName
                    foreach ($c in $cols) {
                        [void]$bulkUpd.ColumnMappings.Add($c, $c)
                    }
                    $bulkUpd.WriteToServer($dtUpdate)
                    $bulkUpd.Close()

                    $updateCols = @()
                    foreach ($c in $cols) {
                        if ($c -ne 'Id') {
                            $updateCols += "T.[$c] = S.[$c]"
                        }
                    }
                    $updateSql = @"
UPDATE T
SET $($updateCols -join ", ")
FROM dbo.[$tbl] T
INNER JOIN $tempTblName S ON T.[Id] = S.[Id];
DROP TABLE $tempTblName;
"@
                    $cmdExecUpd = $tConn.CreateCommand()
                    $cmdExecUpd.CommandTimeout = 120
                    $cmdExecUpd.CommandText = $updateSql
                    $aff = $cmdExecUpd.ExecuteNonQuery()
                    Write-Host "   + Da cap nhat $aff ban ghi moi hon vao [$tbl]" -ForegroundColor Green
                }

                $syncSuccessCount++
            } catch {
                Write-Host "   [LOI] Dong bo bang [$tbl] that bai: $($_.Exception.Message)" -ForegroundColor Red
                $syncFailCount++
            }
        }

        # Bat lai constraint
        Write-Host " [2/3] Kich hoat lai khoa ngoai (CHECK CONSTRAINT ALL)..." -ForegroundColor Yellow
        $cmdEnableAll = $tConn.CreateCommand()
        $cmdEnableAll.CommandText = "EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL;'"
        try {
            $cmdEnableAll.ExecuteNonQuery() | Out-Null
            Write-Host " [3/3] Da xac minh toan bo rang buoc CSDL thanh cong!" -ForegroundColor Green
        } catch {
            Write-Host " [WARN] Bat lai constraint co canh bao: $($_.Exception.Message)" -ForegroundColor Yellow
        }

        Write-Host ""
        Write-Host "=== KET QUA DONG BO CSDL ===" -ForegroundColor Cyan
        Write-Host "Thanh cong: $syncSuccessCount bang" -ForegroundColor Green
        if ($syncFailCount -gt 0) {
            Write-Host "That bai: $syncFailCount bang" -ForegroundColor Red
        }
    } elseif (-not $Apply) {
        Write-Host ""
        Write-Host "[THONG BAO] Day la che do kiem tra (-CheckOnly). De ap dung thay doi vao CSDL, hay chay voi co: .\build\sync-prod-db.ps1 -Apply" -ForegroundColor Cyan
    }

} finally {
    if ($sConn.State -eq 'Open') { $sConn.Close() }
    if ($tConn.State -eq 'Open') { $tConn.Close() }
}

Write-Host "Hoan tat kiem tra va xu ly CSDL." -ForegroundColor Green
