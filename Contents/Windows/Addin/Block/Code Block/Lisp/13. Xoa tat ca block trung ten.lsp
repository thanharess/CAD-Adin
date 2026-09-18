;;; BM_Erase.lsp
;;; Chức năng: Xóa tất cả block cùng tên
;;; Cách dùng: APPLOAD, gõ BLERASE
;;; Sửa lỗi: Mặc định áp dụng cho tất cả instances, loại bỏ prompt selection.

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

(defun bm-explode-or-erase-or-zoom (opt blkname / ss cnt)
  (setq ss (bm-get-all-inserts-by-name blkname))
  (if (not ss)
    (bm-print "❌ Không tìm thấy instance nào để thao tác.")
    (progn
      (setq cnt (sslength ss))
      (cond
        ((= opt "erase")
          (command "_.erase" ss "")
          (bm-print (strcat "🗑️ Đã xóa " (itoa cnt) " instance(s) của \"" blkname "\"."))
        )
      )
    )
  )
)

(defun c:BLERASE (/ blkname)
  (princ "\n=== BM_Erase: Xóa block ===")
  (setq blkname (bm-get-block-name))
  (bm-print (strcat "🔹 Block được chọn: " blkname))
  (bm-explode-or-erase-or-zoom "erase" blkname)
  (princ)
)

(princ "\nGõ BLERASE để chạy lệnh.")
(princ)