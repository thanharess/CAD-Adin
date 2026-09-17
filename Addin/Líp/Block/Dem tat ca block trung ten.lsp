;;; BM_Count.lsp
;;; Chức năng: Đếm số lần xuất hiện của block
;;; Cách dùng: APPLOAD, gõ BLCOUNT
;;; Sửa lỗi: Mặc định đếm tất cả instances, loại bỏ prompt selection.

(defun bm-print (s) (princ (strcat "\n" s)))

(defun bm-get-block-name (/ ent ename edata blkname)
  (setq ent (entsel "\nChọn một block (INSERT): "))
  (if (not ent)
    (progn (bm-print "❌ Bạn chưa chọn block. Thoát.") (exit))
  )
  (setq ename (car ent))
  (setq edata (entget ename))
  (setq blkname (cdr (assoc 2 edata)))
  (if (not blkname)
    (progn (bm-print "❌ Không xác định được tên block.") (exit))
  )
  blkname
)

(defun bm-get-all-inserts-by-name (name / ss)
  (ssget "X" (list (cons 0 "INSERT") (cons 2 name)))
)

(defun bm-count (blkname / ss cnt)
  (setq ss (bm-get-all-inserts-by-name blkname))
  (setq cnt (if ss (sslength ss) 0))
  (bm-print (strcat "📊 Block \"" blkname "\" xuất hiện: " (itoa cnt) " lần."))
  cnt
)

(defun c:BLCOUNT (/ blkname)
  (princ "\n=== BM_Count: Đếm block ===")
  (setq blkname (bm-get-block-name))
  (bm-print (strcat "🔹 Block được chọn: " blkname))
  (bm-count blkname)
  (princ)
)

(princ "\nGõ BLCOUNT để chạy lệnh.")
(princ)