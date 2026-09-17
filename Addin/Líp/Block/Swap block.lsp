;;; ============================================================
;;; BM_SwapBlocks.lsp
;;; Hoán đổi vị trí giữa hai block
;;; Giữ nguyên tỉ lệ và góc xoay của từng block
;;; ============================================================

(vl-load-com)

(defun bm-print (s) (princ (strcat "\n" s)))

(defun c:BLSWAP (/ ent1 ent2 ed1 ed2 p1 p2 r1 r2 sx1 sy1 sz1 sx2 sy2 sz2 n1 n2 doc space)

  (princ "\n=== BM Swap Blocks ===")
  
  ;; Chọn block thứ nhất
  (setq ent1 (entsel "\nChọn block thứ nhất: "))
  (if (not ent1) (progn (bm-print "❌ Hủy chọn.") (exit)))
  (setq ed1 (entget (car ent1)))
  (if (/= (cdr (assoc 0 ed1)) "INSERT")
    (progn (bm-print "❌ Đối tượng đầu tiên không phải block.") (exit))
  )

  ;; Chọn block thứ hai
  (setq ent2 (entsel "\nChọn block thứ hai: "))
  (if (not ent2) (progn (bm-print "❌ Hủy chọn.") (exit)))
  (setq ed2 (entget (car ent2)))
  (if (/= (cdr (assoc 0 ed2)) "INSERT")
    (progn (bm-print "❌ Đối tượng thứ hai không phải block.") (exit))
  )

  ;; Lấy thông tin
  (setq p1 (cdr (assoc 10 ed1)))
  (setq p2 (cdr (assoc 10 ed2)))
  (setq r1 (cdr (assoc 50 ed1)))
  (setq r2 (cdr (assoc 50 ed2)))
  (setq sx1 (cdr (assoc 41 ed1)))
  (setq sy1 (cdr (assoc 42 ed1)))
  (setq sz1 (cdr (assoc 43 ed1)))
  (setq sx2 (cdr (assoc 41 ed2)))
  (setq sy2 (cdr (assoc 42 ed2)))
  (setq sz2 (cdr (assoc 43 ed2)))
  (setq n1 (cdr (assoc 2 ed1)))
  (setq n2 (cdr (assoc 2 ed2)))

  ;; Lấy không gian bản vẽ
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq space (if (= (getvar "CVPORT") 1)
                (vla-get-PaperSpace doc)
                (vla-get-ModelSpace doc)))

  ;; Tạo hai block mới tại vị trí hoán đổi
  (vla-InsertBlock space (vlax-3d-point p2) n1 (if sx1 sx1 1.0) (if sy1 sy1 1.0) (if sz1 sz1 1.0) (if r1 r1 0.0))
  (vla-InsertBlock space (vlax-3d-point p1) n2 (if sx2 sx2 1.0) (if sy2 sy2 1.0) (if sz2 sz2 1.0) (if r2 r2 0.0))

  ;; Xóa block cũ
  (entdel (car ent1))
  (entdel (car ent2))

  (bm-print (strcat "✅ Đã hoán đổi vị trí giữa block \"" n1 "\" và \"" n2 "\"."))
  (princ)
)
(princ "\nGõ BLSWAP để chạy lệnh hoán đổi block.")
(princ)
